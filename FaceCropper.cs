using System.Collections.Concurrent;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using RoboTube;

public enum FaceSelectionStrategy
{
    LargestFace,
    CenterFace,
    LeftMostFace,
    RightMostFace
}


public class FaceCropper
{
    public static async Task<bool> CropToVerticalAsync(string videoTitle, FaceSelectionStrategy strategy)
    {
        try
        {
            return await Task.Run(async () =>
            {
                Console.WriteLine("Start Time: " + DateTime.Now);

                // Model yolları
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "deploy.prototxt");
                var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "res10_300x300_ssd_iter_140000.caffemodel");

                var net = CvDnn.ReadNetFromCaffe(configPath, modelPath);
                net.SetPreferableBackend(Backend.CUDA);
                net.SetPreferableTarget(Target.CUDA);

                var outputVideoPath = GeneralSettings.GetFaceCropVideoDirectory(videoTitle, strategy);
                var inputVideoPath = GeneralSettings.GetTrimVideoDirectory(videoTitle);

                using var cap = new VideoCapture(inputVideoPath);
                if (!cap.IsOpened())
                {
                    Console.WriteLine("Error: Could not open video.");
                    return false;
                }

                int originalWidth = (int)cap.FrameWidth;
                int originalHeight = (int)cap.FrameHeight;
                int totalFrames = (int)cap.FrameCount;
                double fps = cap.Fps;

                int verticalHeight = originalHeight;
                int verticalWidth = (int)(verticalHeight * 9.0 / 16.0);

                if (originalWidth < verticalWidth)
                {
                    Console.WriteLine("Error: Original video width is less than the desired vertical width.");
                    return false;
                }

                int xStart = (originalWidth - verticalWidth) / 2;
                double smoothedXStart = xStart;
                int halfWidth = verticalWidth / 2;

                var fourcc = VideoWriter.FourCC('m', 'p', '4', 'v');

                // Kuyruk tanımı
                var frameQueue = new BlockingCollection<Mat>(boundedCapacity: 30);

                // Arka plan yazma işlemi
                var writerTask = Task.Run(() =>
                {
                    using var writer = new VideoWriter(outputVideoPath, fourcc, fps, new Size(verticalWidth, verticalHeight));
                    foreach (var cropped in frameQueue.GetConsumingEnumerable())
                    {
                        writer.Write(cropped);
                        cropped.Dispose(); // bellek temizliği
                    }
                    Console.WriteLine("Writer thread finished.");
                });

                int frameIndex = 0;
                int lastReportedProgress = -1;
                const double smoothingFactor = 0.5;
                const int faceDetectionInterval = 2;
                
      

                Mat frame = new();
                while (cap.Read(frame))
                {
                    if (frame.Empty())
                        break;

                    Rect[] faces = Array.Empty<Rect>();
                    if (frameIndex % faceDetectionInterval == 0)
                    {
                        using var blob = CvDnn.BlobFromImage(frame, 1.0, new Size(300, 300), new Scalar(104, 177, 123), false, false);
                        net.SetInput(blob);
                        using var output = net.Forward();

                        for (int i = 0; i < output.Size(2); i++)
                        {
                            float confidence = output.At<float>(0, 0, i, 2);
                            if (confidence > 0.5)
                            {
                                int x1 = (int)(output.At<float>(0, 0, i, 3) * frame.Cols);
                                int y1 = (int)(output.At<float>(0, 0, i, 4) * frame.Rows);
                                int x2 = (int)(output.At<float>(0, 0, i, 5) * frame.Cols);
                                int y2 = (int)(output.At<float>(0, 0, i, 6) * frame.Rows);
                                faces = faces.Append(new Rect(x1, y1, x2 - x1, y2 - y1)).ToArray();
                            }
                        }
                    }

                    if (faces.Length > 0)
                    {
                        var mainFace = SelectFace(faces, strategy, originalWidth);
                        int faceCenterX = mainFace.X + mainFace.Width / 2;
                        int targetXStart = faceCenterX - halfWidth;
                        targetXStart = Math.Clamp(targetXStart, 0, originalWidth - verticalWidth);

                        // Yeni titreşim azaltıcı filtre
                        int lastX = xStart;
                        if (Math.Abs(targetXStart - lastX) < 10) // sadece küçük değişimlerde uygula
                        {
                            smoothedXStart = smoothedXStart * (1 - smoothingFactor) + targetXStart * smoothingFactor;
                            xStart = (int)Math.Round(smoothedXStart);
                        }
                        else
                        {
                            // büyük değişim varsa direkt geç
                            xStart = targetXStart;
                            smoothedXStart = targetXStart;
                        }
                    }


                    // Crop ve clone (kuyruğa gönderilecek)
                    var cropped = new Mat(frame, new Rect(xStart, 0, verticalWidth, verticalHeight));
                    frameQueue.Add(cropped.Clone());

                    frameIndex++;
                    int progress = (int)((frameIndex / (double)totalFrames) * 100);
                    if (progress % 5 == 0 && progress != lastReportedProgress)
                    {
                        Console.WriteLine($"Processing: {progress}% ({frameIndex}/{totalFrames} frames)");
                        lastReportedProgress = progress;
                    }
                }

                // Kuyruğu kapat ve writer işini bekle
                frameQueue.CompleteAdding();
                await writerTask;

                Console.WriteLine("Cropping complete. Frames processed: " + frameIndex);
                Console.WriteLine("End Time: " + DateTime.Now);
                return true;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception occurred during cropping: " + ex.Message);
            return false;
        }
    }
    
    private static Rect SelectFace(Rect[] faces, FaceSelectionStrategy strategy, int frameWidth)
    {
        switch (strategy)
        {
            case FaceSelectionStrategy.LargestFace:
                return faces.OrderByDescending(f => f.Width * f.Height).First();

            case FaceSelectionStrategy.CenterFace:
                int centerX = frameWidth / 2;
                return faces.OrderBy(f => Math.Abs((f.X + f.Width / 2) - centerX)).First();

            case FaceSelectionStrategy.LeftMostFace:
                return faces.OrderBy(f => f.X).First();

            case FaceSelectionStrategy.RightMostFace:
                return faces.OrderByDescending(f => f.X + f.Width).First();

            default:
                return faces[0];
        }
    }



}

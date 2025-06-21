using OpenCvSharp;
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
        return await Task.Run(() =>
        {
            Console.WriteLine("Start Time: " + DateTime.Now);

            var CascadeClassifierPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "haarcascade_frontalface_default.xml");
            var faceCascade = new CascadeClassifier(CascadeClassifierPath);


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
            using var writer = new VideoWriter(outputVideoPath, fourcc, fps, new Size(verticalWidth, verticalHeight));

            Console.WriteLine($"Original: {originalWidth}x{originalHeight}, Crop: {verticalWidth}x{verticalHeight}");

            int frameIndex = 0;
            int lastReportedProgress = -1;
            const double smoothingFactor = 0.2;

            Mat frame = new Mat();
            while (cap.Read(frame))
            {
                if (frame.Empty())
                    break;

                using var gray = new Mat();
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

                Rect[] faces = new Rect[0];
                if (frameIndex % 2 == 0)
                {
                    faces = faceCascade.DetectMultiScale(gray, 1.1, 5, HaarDetectionTypes.ScaleImage, new Size(30, 30));
                }

                if (faces.Length > 0)
                {
                    var mainFace = SelectFace(faces, strategy, originalWidth);
                    int faceCenterX = mainFace.X + mainFace.Width / 2;

                    int targetXStart = faceCenterX - halfWidth;
                    targetXStart = Math.Max(0, targetXStart);
                    targetXStart = Math.Min(originalWidth - verticalWidth, targetXStart);

                    smoothedXStart = smoothedXStart * (1 - smoothingFactor) + targetXStart * smoothingFactor;
                    xStart = (int)Math.Round(smoothedXStart);
                }

                var cropped = new Mat(frame, new Rect(xStart, 0, verticalWidth, verticalHeight));
                writer.Write(cropped);

                frameIndex++;

                int progress = (int)((frameIndex / (double)totalFrames) * 100);
                if (progress % 5 == 0 && progress != lastReportedProgress)
                {
                    Console.WriteLine($"Processing: {progress}% ({frameIndex}/{totalFrames} frames)");
                    lastReportedProgress = progress;
                }
            }

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

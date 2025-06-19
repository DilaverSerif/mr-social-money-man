using OpenCvSharp;


public class FaceCropper
{
       public static void CropToVertical(string inputVideoPath)
    {
        var CascadeClassifierPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "haarcascade_frontalface_default.xml");
        var faceCascade = new CascadeClassifier(CascadeClassifierPath);
        var outputVideoPath =  Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads", "test", "face_test.mp4");

        using var cap = new VideoCapture(inputVideoPath);
        if (!cap.IsOpened())
        {
            Console.WriteLine("Error: Could not open video.");
            return;
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
            return;
        }

        int xStart = (originalWidth - verticalWidth) / 2;
        int xEnd = xStart + verticalWidth;
        int halfWidth = verticalWidth / 2;

        var fourcc = VideoWriter.FourCC('m', 'p', '4', 'v');
        using var writer = new VideoWriter(outputVideoPath, fourcc, fps, new Size(verticalWidth, verticalHeight));

        Console.WriteLine($"Original: {originalWidth}x{originalHeight}, Crop: {verticalWidth}x{verticalHeight}");

        int frameIndex = 0;

        Mat frame = new Mat();
        while (cap.Read(frame))
        {
            if (frame.Empty())
                break;

            var gray = new Mat();
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

            Rect[] faces = faceCascade.DetectMultiScale(gray, 1.1, 5, HaarDetectionTypes.ScaleImage, new Size(30, 30));

            if (faces.Length > 0)
            {
                var mainFace = faces[0];
                int faceCenterX = mainFace.X + mainFace.Width / 2;

                int newXStart = faceCenterX - halfWidth;
                newXStart = Math.Max(0, newXStart);
                newXStart = Math.Min(originalWidth - verticalWidth, newXStart);
                xStart = newXStart;
                xEnd = xStart + verticalWidth;
            }

            var cropped = new Mat(frame, new Rect(xStart, 0, verticalWidth, verticalHeight));
            writer.Write(cropped);
            frameIndex++;
        }

        Console.WriteLine("Cropping complete. Frames processed: " + frameIndex);
    }

}

using OpenCvSharp;
using System;
using System.Drawing;

public class FaceCropper
{
    public static int DetectFaceCenterX(string videoPath)
    {
        if (!File.Exists(videoPath))
            throw new Exception("Video dosyası bulunamadı: " + videoPath);

        var cascadeFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "haarcascade_frontalface_default.xml");
        if (!File.Exists(cascadeFilePath))
            throw new Exception("Cascade dosyası bulunamadı: " + cascadeFilePath);

        using var capture = new VideoCapture(videoPath);
        using var mat = new Mat();
        if (!capture.Read(mat) || mat.Empty())
            throw new Exception("İlk kare okunamadı.");

        using var gray = new Mat();
        Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

        var faceCascade = new CascadeClassifier(cascadeFilePath);
        var faces = faceCascade.DetectMultiScale(
            image: gray,
            scaleFactor: 1.1,
            minNeighbors: 5,
            flags: HaarDetectionTypes.ScaleImage
        );

        if (faces.Length == 0)
            throw new Exception("Yüz bulunamadı.");

        var face = faces[0];
        return face.X + face.Width / 2;
    }

}

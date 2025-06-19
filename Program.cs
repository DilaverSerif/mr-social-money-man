using RoboTube;

public class Program
{
    public static async Task Main(string[] args)
    {

        if (args.Contains("--testedit"))
        {
            Console.WriteLine("Test edit modu aktif!");
            VideoEditor.ResizeVideoWithPreset(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test", "test.mp4"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output", "output.mp4"), VideoPreset.Portrait);
            return;
        }
        else if (args.Contains("--testmp3"))
        {
            Console.WriteLine("Test mp3 modu aktif!");
            var inputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test", "test.mp4");
            var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output", "output.mp3");
            if (VideoEditor.ConvertVideoToMp3(inputPath, outputPath))
            {
                Console.WriteLine("MP3 dönüştürme başarılı: " + outputPath);
            }
            else
            {
                Console.WriteLine("MP3 dönüştürme başarısız.");
            }
            return;
        }
        else if (args.Contains("--testscroll"))
        {
            Console.WriteLine("Test scroll modu aktif!");
            VideoAnimations.AddScrollingTextToVideo(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test", "test.mp4"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output", "output.mp4"),
                "RoboTube - YouTube Video Editor",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "font.ttf"),
                24, "white", 50, 100);
            return;
        }

        else if (args.Contains("--testface"))
        {
            var testVideoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads", "test", "test.mp4");
            Console.WriteLine("Test video yolu: " + testVideoPath);
            FaceCropper.CropToVertical(testVideoPath);
            Console.WriteLine("Yüz tespiti ve kırpma işlemi tamamlandı.");
            return;
            var testfaceCenterX = 100;
            Console.WriteLine("Yüz merkezi X koordinatı: " + testfaceCenterX);

            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads", "test", "output", "testface.mp4")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads", "test"));
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads", "test", "output"));
            }

            VideoEditor.CropToVertical(
                testVideoPath,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads", "test", "output", "testface.mp4"),
                testfaceCenterX,
                1080, // Yükseklik
                1920); // Genişlik

            return;
        }
        else if (args.Contains("--testdownload"))
        {
            Console.WriteLine("Test download modu aktif!");
            var testvideoUrl = "https://www.youtube.com/watch?v=_sY3FrDJGH8";
            var testgetVideoTitle = await YoutubeDownloader.GetVideoTitleAsync(testvideoUrl);
            Console.WriteLine("Test video başlığı: " + testgetVideoTitle);
            var testdownloadVideoPath = await YoutubeDownloader.DownloadVideoAsync(testvideoUrl, GeneralSettings.GetDownloadDirectory(testgetVideoTitle));
            return;
        }


        Console.WriteLine("Normal mod");
        var videoUrl = "https://www.youtube.com/watch?v=66adFeve3ME";
        var getVideoTitle = await YoutubeDownloader.GetVideoTitleAsync(videoUrl);
        var downloadVideoPath = await YoutubeDownloader.DownloadVideoAsync(videoUrl, GeneralSettings.GetDownloadDirectory(getVideoTitle));
        Console.WriteLine("Video indirildi: " + downloadVideoPath);
        var wavExportPath = await VideoEditor.ConvertVideoToWavAsync(downloadVideoPath, GeneralSettings.GetWavExportPath(getVideoTitle));
        await WhisperBrain.TranscribeAudioWithTimestamps(wavExportPath, GeneralSettings.GetOutputDirectoryForJson(getVideoTitle));
        //var faceCenterX = FaceCropper.DetectFaceCenterX(downloadVideoPath);
        //Console.WriteLine("Yüz merkezi X koordinatı: " + faceCenterX);

        // VideoEditor.CropToVertical(
        //     downloadVideoPath,
        //     GeneralSettings.GetOutputDirectory(getVideoTitle),
        //     faceCenterX,
        //     1080, // Yükseklik
        //     1920); // Genişlik

        //VideoEditor.ResizeVideoWithPreset(videoDownloanedPath, GeneralSettings.GetOutputDirectory(getVideoTitle), VideoPreset.Portrait);
    }
}
using mrmoneyman;
using RoboTube;
using OpenCvSharp;
using System.Runtime.InteropServices;
using Mediapipe.FaceGeometry;
public class Program
{

    [DllImport("OpenCvSharpExtern")]
    public static extern int cuda_GetCudaEnabledDeviceCount();

    public static async Task Main(string[] args)
    {

        if (args.Contains("--testedit"))
        {
            Console.WriteLine("Test edit modu aktif!");

            return;
        }
        else if (args.Contains("--testcuda"))
        {
            Console.WriteLine("CUDA destekli cihaz sayısı: " + cuda_GetCudaEnabledDeviceCount());
        }
        else if (args.Contains("trimvideo"))
        {
            var videoTitle = "En İyi Fallout Şehri Hangisi?";
            var segment = new VideoSegment { Start = 60, End = 120 };
            await VideoEditor.TrimFromJsonAsync(segment, videoTitle);
            return;
        }
        else if (args.Contains("--testgemini"))
        {
            // var faceMesh = new FaceMeshSolution();
            // faceMesh.Initialize(FaceMeshConfig.DefaultConfig(isGpu: true));

            // var bytes = File.ReadAllBytes("photo.jpg");
            // var result = await faceMesh.ProcessImageAsync(bytes);

            // landmark'lar burada
            // foreach (var face in result.MultiFaceLandmarks)
            // {
            //     foreach (var lm in face.Landmark)
            //         Console.WriteLine($"{lm.X}, {lm.Y}, {lm.Z}");
            // }
            return;
        }
        else if (args.Contains("--testtranscript"))
        {
        }
        else if (args.Contains("--testwhisper"))
        {
            await WhisperBrain.TranscribeAudioWithTimestamps("YKSYE_GIREN_PROFESOR__Universitelerin_Sorunu_Ne");
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
            await VideoSubtitleGenerator.ProcessVideoAsync("YKSYE_GIREN_PROFESOR__Universitelerin_Sorunu_Ne", FaceSelectionStrategy.LargestFace, SubtitleStyle.Cartoon);
            return;
        }

        else if (args.Contains("--testface"))
        {
            await FaceCropper.CropToVerticalAsync("YKSYE_GIREN_PROFESOR__Universitelerin_Sorunu_Ne", FaceSelectionStrategy.LargestFace);
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
        else if (args.Contains("--testvoice"))
        {
            await VideoEditor.CombineVideoAndWav("YKSYE_GIREN_PROFESOR__Universitelerin_Sorunu_Ne");
            return;
        }

        var nowTime = DateTime.Now;
        Console.WriteLine("Program başlatıldı..." +nowTime);
        var videoUrl = "https://www.youtube.com/watch?v=KpJ_QcupPzA";
        var videoTitleName = await YoutubeDownloader.GetVideoTitleAsync(videoUrl);
        if (videoTitleName == string.Empty)
        {
            Console.WriteLine("Video başlığı alınamadı. Lütfen geçerli bir YouTube URL'si girin.");
            return;
        }

        var downloadVideoAsync = await YoutubeDownloader.DownloadVideoAsync(videoUrl, videoTitleName);

        if (!downloadVideoAsync)
        {
            Console.WriteLine("Video indirilemedi. Lütfen geçerli bir YouTube URL'si girin.");
            return;
        }
        Console.WriteLine("Video indirildi: " + GeneralSettings.GetDownloadDirectory(videoTitleName));

        var convertVideoToWavAsync = await VideoEditor.ConvertVideoToWavAsync(videoTitleName);

        if (!convertVideoToWavAsync)
        {
            Console.WriteLine("Video WAV formatına dönüştürülemedi. Lütfen geçerli bir video dosyası girin.");
            return;
        }
        Console.WriteLine("Video WAV formatına dönüştürüldü: " + GeneralSettings.GetWavByOutputPath(videoTitleName));

        var transcriptPath = await WhisperBrain.TranscribeAudioWithTimestamps(videoTitleName);

        Console.WriteLine("Transkript dosyası oluşturuldu: " + transcriptPath);

        var geminiResponse = await Brain_Gemini.TalkWithGemini(videoTitleName);
        Console.WriteLine("Gemini yanıtladı");
        var result = await VideoEditor.TrimFromJsonAsync(geminiResponse, videoTitleName);

        await FaceCropper.CropToVerticalAsync(videoTitleName, FaceSelectionStrategy.LargestFace);

        Console.WriteLine("Video düzenleme tamamlandı: " + GeneralSettings.GetFaceCropVideoDirectory(videoTitleName, FaceSelectionStrategy.LargestFace));
        await VideoSubtitleGenerator.ProcessVideoAsync(videoTitleName, FaceSelectionStrategy.LargestFace, SubtitleStyle.Cartoon);
        Console.WriteLine("Video altyazı oluşturma tamamlandı");
        //Console.WriteLine("Yüz merkezi X koordinatı: " + faceCenterX);
        await VideoEditor.CombineVideoAndWav(videoTitleName);

        var diff = DateTime.Now - nowTime;
        Console.WriteLine($"İşlem süresi: {diff.TotalSeconds} saniye");
        // VideoEditor.CropToVertical(
        //     downloadVideoPath,
        //     GeneralSettings.GetOutputDirectory(getVideoTitle),
        //     faceCenterX,
        //     1080, // Yükseklik
        //     1920); // Genişlik

        //VideoEditor.ResizeVideoWithPreset(videoDownloanedPath, GeneralSettings.GetOutputDirectory(getVideoTitle), VideoPreset.Portrait);
    }
}
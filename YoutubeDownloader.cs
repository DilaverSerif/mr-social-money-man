using System.Diagnostics;

namespace RoboTube;

public static class YoutubeDownloader
{
    public static async Task<string> DownloadVideoAsync(string videoUrl, string downloadPath)
{
    var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "yt-dlp");

    // İndirme dizini yoksa oluştur
    if (!Directory.Exists(downloadPath))
        Directory.CreateDirectory(downloadPath);

    var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";
    var outputTemplate = Path.Combine(downloadPath, "%(title)s.%(ext)s");

    var psi = new ProcessStartInfo
    {
        FileName = exePath,
        Arguments = $"\"{videoUrl}\" -o \"{outputTemplate}\" -f mp4 --restrict-filenames --user-agent \"{userAgent}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    var tcs = new TaskCompletionSource<bool>();
    string errorOutput = "";

    using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

    process.OutputDataReceived += (s, e) =>
    {
        if (!string.IsNullOrWhiteSpace(e.Data))
            Console.WriteLine("YT-DLP: " + e.Data);
    };

    process.ErrorDataReceived += (s, e) =>
    {
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            Console.Error.WriteLine("YT-DLP HATA: " + e.Data);
            errorOutput += e.Data + "\n";
        }
    };

    process.Exited += (s, e) =>
    {
        tcs.TrySetResult(true);
    };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    await tcs.Task;
    await process.WaitForExitAsync();

    if (!string.IsNullOrEmpty(errorOutput))
        throw new Exception("yt-dlp hata verdi:\n" + errorOutput);

    var videoPath = Directory.GetFiles(downloadPath, "*.mp4")
        .OrderByDescending(f => new FileInfo(f).CreationTime)
        .FirstOrDefault();

    if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
        throw new Exception($"Video indirilemedi. İndirme dizini: {downloadPath}");

    return videoPath;
}



    public static async Task<string> GetVideoTitleAsync(string videoUrl)
    {
        var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "yt-dlp");

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"\"{videoUrl}\" --get-title",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process { StartInfo = psi };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string errors = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            string title = output.Trim();

            if (string.IsNullOrEmpty(title))
            {
                Console.WriteLine("Video başlığı alınamadı.");
                if (!string.IsNullOrWhiteSpace(errors))
                    Console.Error.WriteLine("YT-DLP HATA: " + errors);

                throw new Exception("Video başlığı alınamadı.");
            }

            Console.WriteLine($"Video başlığı: {title}");
            return title;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Hata oluştu: " + ex.Message);
            throw;
        }
    }

}
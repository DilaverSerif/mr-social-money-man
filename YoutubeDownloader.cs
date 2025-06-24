using System.Diagnostics;

namespace RoboTube;

public static class YoutubeDownloader
{
    public static async Task<bool> DownloadVideoAsync(string videoUrl, string videoTitleName)
    {
        var exePath = GeneralSettings.GetTYTDLPPath();
        
        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";
        var outputTemplate = GeneralSettings.GetVideoDirectory(videoTitleName);

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"\"{videoUrl}\" -o \"{outputTemplate}\" -f \"bv*[height=1080]+ba\" --merge-output-format mp4 --restrict-filenames --user-agent \"{userAgent}\"",
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
                Console.WriteLine("\rYT-DLP: " + e.Data);
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
        {
            throw new Exception("yt-dlp hata verdi:\n" + errorOutput);
            return false;
        }
        
        return true;
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
                    return string.Empty;

                return string.Empty;
            }

            Console.WriteLine($"Video başlığı: {title}");
            return title.NormalizeString();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Hata oluştu: " + ex.Message);
            return string.Empty;
            throw;
        }
    }

}
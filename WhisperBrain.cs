using System.Diagnostics;
using System.Text.Json;

namespace RoboTube;

public static class WhisperBrain
{
    public static async Task<bool> TranscribeAudioWithTimestamps(string videoTitle)
    {
        Console.WriteLine("Transkription işlemi başlatılıyor: " + videoTitle);
        var audioFilePath = GeneralSettings.GetWavByOutputPath(videoTitle);
        var outputDir = GeneralSettings.GetOutputDirectoryForJson(videoTitle, "transcript");

        var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "whisper-cli");
        var modelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "ggml-base.bin");

        var language = "tr";
        var outputFormat = "otxt";
        var maxLen = 70; // örnek: her segment maksimum 70 karakter
        var threads = Environment.ProcessorCount; // ya da sabit sayı örn: 8
        var arguments = $"-m \"{modelsPath}\" -f \"{audioFilePath}\" -l {language} -{outputFormat} -ml {maxLen} --split-on-word -t {threads}";


        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        string stdOutput = await process.StandardOutput.ReadToEndAsync();
        string errors = await process.StandardError.ReadToEndAsync();
        Console.WriteLine(errors);

        await File.WriteAllTextAsync(outputDir, stdOutput.ToSubtitleJsonList());

        await process.WaitForExitAsync();
        return true;
    }
}
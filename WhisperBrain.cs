using System.Diagnostics;
using System.Text.Json;

namespace RoboTube;

public class WhisperBrain
{
    public static async Task TranscribeAudioWithTimestamps(string audioFilePath,string outputPath)
    {
        var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "whisper-cli");
        var modelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "ggml-base.bin");

        var arguments = $"-m \"{modelsPath}\" -f \"{audioFilePath}\" -of json";

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
        string errors = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var jsonOutputPath = Path.Combine(outputPath + "/transcript.json");

        if (File.Exists(jsonOutputPath))
        {
            var jsonContent = await File.ReadAllTextAsync(jsonOutputPath);
            var doc = JsonDocument.Parse(jsonContent);
            var segments = doc.RootElement.GetProperty("segments");

            foreach (var segment in segments.EnumerateArray())
            {
                double start = segment.GetProperty("start").GetDouble();
                double end = segment.GetProperty("end").GetDouble();
                string text = segment.GetProperty("text").GetString() ?? "";

                Console.WriteLine($"[{start:0.00} - {end:0.00}] {text}");
            }
        }
        else
        {
            // Eğer JSON dosyası bulunamazsa oluştur
            var jsonContent = await process.StandardOutput.ReadToEndAsync();
            await File.WriteAllTextAsync(jsonOutputPath, jsonContent);
            Console.WriteLine("JSON dosyası oluşturuldu: " + jsonOutputPath);
        }
    }

}
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using RoboTube;

public static class VideoSubtitleGenerator
{
    public static async Task ProcessVideoAsync(string videoTitle, FaceSelectionStrategy faceSelectionStrategy, SubtitleStyle style)
    {
        try
        {
            Console.WriteLine("İşlem başladı...");

            var ffmpegPath = GeneralSettings.GetFFmpegPath();
            var transcriptPath = GeneralSettings.GetTranscriptByVideoTitle(videoTitle);
            var faceCropVideoPath = GeneralSettings.GetFaceCropVideoDirectory(videoTitle, faceSelectionStrategy);
            var geminiResponsePath = GeneralSettings.GetGeminiJsonPath(videoTitle);
            var outputPath = GeneralSettings.GetSubtitleVideoDirectory(videoTitle);
            var assPath = GeneralSettings.GetSubtitleStylePath(style);
            var fontFolder = GeneralSettings.GetFolderFontPath();

            Console.WriteLine($"Font yolu: {fontFolder}");
            Console.WriteLine($"Font dosyası mevcut mu: {File.Exists(fontFolder)}");

            if (!File.Exists(assPath))
            {
                Console.WriteLine($"Stil dosyası bulunamadı: {assPath}");
                return;
            }

            var geminiJson = await File.ReadAllTextAsync(geminiResponsePath);
            var transcriptJson = await File.ReadAllTextAsync(transcriptPath);

            var gemini = JsonSerializer.Deserialize<VideoSegment>(geminiJson);
            var transcript = JsonSerializer.Deserialize<List<TranscriptItem>>(transcriptJson);

            if (gemini == null || transcript == null || transcript.Count == 0)
            {
                Console.WriteLine("Gemini veya transcript boş.");
                return;
            }

            var selectedLines = transcript
                .Where(t => t.Start >= gemini.Start && t.End <= gemini.End)
                .ToList();

            if (selectedLines.Count == 0)
            {
                Console.WriteLine("Uygun transcript satırı bulunamadı.");
                return;
            }

            var lines = (await File.ReadAllLinesAsync(assPath)).ToList();
            var eventIndex = lines.FindIndex(l => l.Trim().Equals("[Events]", StringComparison.OrdinalIgnoreCase));

            if (eventIndex == -1 || eventIndex + 1 >= lines.Count || !lines[eventIndex + 1].StartsWith("Format"))
            {
                Console.WriteLine("[Events] veya Format bölümü eksik.");
                return;
            }

            lines = lines.Take(eventIndex + 2).ToList();

            foreach (var line in selectedLines)
            {
                string styleName = "Default";
                string text = line.Text;

                if (text.StartsWith("PVO:", StringComparison.OrdinalIgnoreCase))
                {
                    lines.Add($"Dialogue: 0,{ToAssTimestamp(line.Start - gemini.Start)},{ToAssTimestamp(line.End - gemini.Start)},RedTag,,0,0,0,,PVO:");
                    lines.Add($"Dialogue: 0,{ToAssTimestamp(line.Start - gemini.Start)},{ToAssTimestamp(line.End - gemini.Start)},Default,,0,0,0,,{text.Substring(4).Trim()}");
                }
                else
                {
                    lines.Add($"Dialogue: 0,{ToAssTimestamp(line.Start - gemini.Start)},{ToAssTimestamp(line.End - gemini.Start)},{styleName},,0,0,0,,{text}");
                }
            }

            await File.WriteAllLinesAsync(assPath, lines);

            // var start = gemini.Start.ToString(System.Globalization.CultureInfo.InvariantCulture);
            // var duration = (gemini.End - gemini.Start).ToString(System.Globalization.CultureInfo.InvariantCulture);

            var ffmpegArgs = $"-i \"{faceCropVideoPath}\" -vf \"subtitles={assPath}:fontsdir={fontFolder}\" -c:a copy \"{outputPath}\"";

            await RunFFmpegAsync(ffmpegArgs);

            Console.WriteLine("Video başarıyla işlendi.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Hata oluştu: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }


    private static string ToAssTimestamp(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return $"{ts.Hours:D1}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds / 10:D2}";
    }

    private static async Task RunFFmpegAsync(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = GeneralSettings.GetFFmpegPath(),
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        Console.WriteLine("FFmpeg çıktı:\n" + outputBuilder.ToString());
        Console.WriteLine("FFmpeg hata:\n" + errorBuilder.ToString());

        if (process.ExitCode != 0)
            throw new Exception($"FFmpeg hata kodu: {process.ExitCode}");
    }
}



public enum SubtitleStyle
{
    Cartoon,
    Minimal,
    BoldRed
}

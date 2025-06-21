using System.Diagnostics;
using System.Text.Json;
using RoboTube;

public static class VideoSubtitleGenerator
{
    public static void ProcessVideo(string videoTitle, FaceSelectionStrategy faceSelectionStrategy)
    {
        try
        {
            Console.WriteLine("ProcessVideo başladı...");

            var ffmpegPath = GeneralSettings.GetFFmpegPath();
            Console.WriteLine($"FFmpeg yolu: {ffmpegPath}");

            var transcriptPath = GeneralSettings.GetTranscriptByVideoTitle(videoTitle);
            Console.WriteLine($"Transcript yolu: {transcriptPath}");

            var faceCropVideoPath = GeneralSettings.GetFaceCropVideoDirectory(videoTitle, faceSelectionStrategy);
            Console.WriteLine($"Yüz kırpılmış video yolu: {faceCropVideoPath}");

            var geminiResponsePath = GeneralSettings.GetGeminiJsonPath(videoTitle);
            Console.WriteLine($"Gemini JSON yolu: {geminiResponsePath}");

            var outputPath = GeneralSettings.GetFinishVideoDirectory(videoTitle);
            Console.WriteLine($"Çıktı video yolu: {outputPath}");

            // 1. Parse JSON files
            Console.WriteLine("JSON dosyaları okunuyor...");
            var gemini = JsonSerializer.Deserialize<VideoSegment>(File.ReadAllText(geminiResponsePath));
            var transcript = JsonSerializer.Deserialize<List<TranscriptItem>>(File.ReadAllText(transcriptPath));

            if (gemini == null)
            {
                Console.WriteLine("Gemini JSON boş veya hatalı.");
                return;
            }

            if (transcript == null || transcript.Count == 0)
            {
                Console.WriteLine("Transcript JSON boş veya hatalı.");
                return;
            }

            // 2. Filter transcript by Gemini Start-End
            Console.WriteLine("Transcript filtreleniyor...");
            var selectedLines = transcript
                .Where(t => t.Start >= gemini.Start && t.End <= gemini.End)
                .ToList();

            if (selectedLines.Count == 0)
            {
                Console.WriteLine("Filtrelenen transcript satırı bulunamadı.");
                return;
            }

            // 3. Create .srt subtitle file
            var srtFilePath = "subtitles.srt";
            Console.WriteLine("SRT dosyası yazılıyor...");
            using (var writer = new StreamWriter(srtFilePath))
            {
                for (int i = 0; i < selectedLines.Count; i++)
                {
                    var line = selectedLines[i];
                    writer.WriteLine(i + 1);
                    writer.WriteLine($"{ToSrtTimestamp(line.Start - gemini.Start)} --> {ToSrtTimestamp(line.End - gemini.Start)}");
                    writer.WriteLine(line.Text);
                    writer.WriteLine();
                }
            }

            Console.WriteLine("SRT dosyası oluşturuldu: " + srtFilePath);

            // 4. Trim video and burn subtitles using ffmpeg
            var ffmpegArgs = $"-ss {gemini.Start} -i \"{faceCropVideoPath}\" -t {gemini.End - gemini.Start} -vf subtitles=\"{srtFilePath}\" -c:a copy \"{outputPath}\"";
            Console.WriteLine("FFmpeg başlatılıyor...");
            Console.WriteLine("FFmpeg argümanları: " + ffmpegArgs);

            RunFFmpeg(ffmpegArgs);

            Console.WriteLine("ProcessVideo tamamlandı.");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine("Video işleme sırasında bir hata oluştu:");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            return;
        }
    }

    private static string ToSrtTimestamp(double timeInSeconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(timeInSeconds);
        return $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2},{t.Milliseconds:D3}";
    }

    private static void RunFFmpeg(string arguments)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = GeneralSettings.GetFFmpegPath(),
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        try
        {
            using (Process process = Process.Start(psi))
            {
                if (process == null)
                {
                    throw new Exception("FFmpeg process başlatılamadı.");
                }

                Console.WriteLine("FFmpeg çalışıyor..." + DateTime.Now);

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Console.WriteLine("FFmpeg çıktı: " + output);
                Console.WriteLine("FFmpeg hata: " + error);

                if (process.ExitCode != 0)
                {
                    throw new Exception($"FFmpeg hata kodu: {process.ExitCode}\nHata çıktısı: {error}");
                }

                Console.WriteLine("FFmpeg işlemi başarıyla tamamlandı." + DateTime.Now);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("FFmpeg çalıştırılırken hata oluştu: " + ex.Message);
        }
    }
}

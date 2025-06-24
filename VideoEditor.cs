using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace RoboTube
{
    public enum VideoPreset
    {
        Portrait,   // 1080x1920
        Landscape,  // 1920x1080
        Square      // 1080x1080
    }
    public static class VideoEditor
    {
        public static bool ConvertVideoToMp3(string inputPath, string outputPath)
        {
            string arguments = $"-i \"{inputPath}\" -vn -acodec libmp3lame -q:a 2 \"{outputPath}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GeneralSettings.GetFFmpegPath(),
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                string errorOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                    return true;
                else
                {
                    Console.WriteLine("FFmpeg Hatası: " + errorOutput);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("İşlem başlatılamadı: " + ex.Message);
                return false;
            }
        }
        public static async Task<bool> ConvertVideoToWavAsync(string videoTitle)
        {
            Console.WriteLine("Video WAV formatına dönüştürülüyor: " + videoTitle);
            var inputPath = GeneralSettings.GetVideoDirectory(videoTitle);
            var outputPath = GeneralSettings.GetWavByOutputPath(videoTitle);

            string arguments = $"-i \"{inputPath}\" -vn -acodec pcm_s16le -ar 44100 -ac 2 \"{outputPath}\"";

            var psi = new ProcessStartInfo
            {
                FileName = GeneralSettings.GetFFmpegPath(),
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var process = new Process();
                process.StartInfo = psi;
                process.EnableRaisingEvents = true;

                var stdError = new StringBuilder();
                var tcs = new TaskCompletionSource<bool>();

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                        stdError.AppendLine(e.Data);
                };

                process.Exited += (s, e) =>
                {
                    tcs.TrySetResult(true);
                };

                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                await tcs.Task;
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                    return true;

                Console.WriteLine("FFmpeg Hatası:\n" + stdError.ToString());
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("İşlem başlatılamadı: " + ex.Message);
                return false;
            }
        }

        public static async Task<bool> CombineVideoAndWav(string videoTitle, VideoPreset preset = VideoPreset.Landscape)
        {
            Console.WriteLine("Video MP4 formatına dönüştürülüyor: " + videoTitle);

            var videoPath = GeneralSettings.GetSubtitleVideoDirectory(videoTitle);
            var wavInputPath = GeneralSettings.GetWavByOutputPath(videoTitle);
            var outputPath = GeneralSettings.GetFinishVideoDirectory(videoTitle);
            var geminiAnswerPath = GeneralSettings.GetGeminiJsonPath(videoTitle);



            try
            {
                // JSON verisini oku
                string json = await File.ReadAllTextAsync(geminiAnswerPath);
                var doc = JsonDocument.Parse(json);
                double start = doc.RootElement.GetProperty("Start").GetDouble();
                double end = doc.RootElement.GetProperty("End").GetDouble();
                double duration = end - start;


                string tempWav = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "_cut.wav");
                Console.WriteLine("Geçici WAV dosyası: " + tempWav);
                string cutAudioArgs = $"-i \"{wavInputPath}\" -ss {start} -to {end} -c:a pcm_s16le \"{tempWav}\"";
                await RunFFmpegAsync(cutAudioArgs);

                string outputArgs = $"-i \"{videoPath}\" -i \"{tempWav}\" " +
                                    $"-map 0:v -map 1:a -c:v copy -c:a aac -shortest \"{outputPath}\"";
                await RunFFmpegAsync(outputArgs);

                // Geçici dosyayı sil
                File.Delete(tempWav);

                Console.WriteLine("Video başarıyla dönüştürüldü.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata: " + ex.Message);
                return false;
            }
        }

        private static async Task RunFFmpegAsync(string arguments)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = GeneralSettings.GetFFmpegPath(),
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = ffmpeg };
            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"FFmpeg hata verdi:\n{error}");
        }


        public static async Task<bool> TrimFromJsonAsync(VideoSegment segment, string videoTitle)
        {
            var outPath = GeneralSettings.GetTrimVideoDirectory(videoTitle);
            var inputPath = GeneralSettings.GetVideoDirectory(videoTitle);
            var wavInputPath = GeneralSettings.GetWavByOutputPath(videoTitle);

            if (string.IsNullOrWhiteSpace(outPath))
            {
                Console.WriteLine("Çıkış yolu boş veya geçersiz.");
                return false;
            }


            double duration = segment.End - segment.Start;

            string start = segment.Start.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string dur = duration.ToString(System.Globalization.CultureInfo.InvariantCulture);

            string args = $"-ss {start} -t {dur} -i \"{inputPath}\" -ss {start} -t {dur} -i \"{wavInputPath}\" -map 0:v:0 -map 1:a:0 -c:v libx264 -preset veryfast -c:a aac -b:a 128k -shortest \"{outPath}\"";


            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GeneralSettings.GetFFmpegPath(),
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            string stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                Console.WriteLine("FFmpeg hata çıktısı:" + stderr);
                return false;
            }

            Console.WriteLine($"Video kesme işlemi tamamlandı: {outPath}");
            return true;
        }


    }
}

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
        public static void CropToVertical(string inputPath, string outputPath, int faceCenterX, int width, int height)
        {
            Console.WriteLine("Video kırpma işlemi başlatılıyor...");
            string ffmpegPath = GeneralSettings.GetFFmpegPath();

            // Maksimum kırpma yüksekliği: mevcut yüksekliği aşmasın
            int verticalHeight = height;

            // İdeal dikey format oranı 9:16
            int idealWidth = (int)(verticalHeight * 9.0 / 16.0);

            // Eğer hesaplanan genişlik, videonun genişliğini aşıyorsa yeniden hesapla
            if (idealWidth > width)
            {
                idealWidth = width;
                verticalHeight = (int)(idealWidth * 16.0 / 9.0);
            }

            int cropX = Math.Max(0, faceCenterX - idealWidth / 2);
            if (cropX + idealWidth > width)
                cropX = width - idealWidth;

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            string args = $"-i \"{inputPath}\" -vf \"crop={idealWidth}:{verticalHeight}:{cropX}:0\" -c:v libx264 -preset veryfast -c:a aac \"{outputPath}\"";

            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            string errorOutput = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Console.WriteLine("FFmpeg hata çıktısı:");
            Console.WriteLine(errorOutput);
            Console.WriteLine("Video kırpma tamamlandı.");
        }

        public static async Task<bool> TrimFromJsonAsync(VideoSegment segment, string videoTitle)
        {
            var outPath = GeneralSettings.GetTrimVideoDirectory(videoTitle);
            var inputPath = GeneralSettings.GetVideoDirectory(videoTitle);

            if (string.IsNullOrWhiteSpace(outPath))
            {
                Console.WriteLine("Çıkış yolu boş veya geçersiz.");
                return false;
            }


            double duration = segment.End - segment.Start;

            string start = segment.Start.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string dur = duration.ToString(System.Globalization.CultureInfo.InvariantCulture);

            string args = $"-i \"{inputPath}\" -ss {start} -t {dur} -c copy \"{outPath}\"";

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

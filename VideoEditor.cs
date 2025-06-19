using System.Diagnostics;
using System.Text;

namespace RoboTube
{
    public enum VideoPreset
    {
        Portrait,   // 1080x1920
        Landscape,  // 1920x1080
        Square      // 1080x1080
    }
    public class VideoEditor
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
        public static async Task<string> ConvertVideoToWavAsync(string inputPath, string outputPath)
        {
            // Eğer outputPath uzantısızsa .wav ekle
            if (string.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
                outputPath = Path.Combine(outputPath, "audio.wav");

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
                using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

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
                    return outputPath;

                Console.WriteLine("FFmpeg Hatası:\n" + stdError.ToString());
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine("İşlem başlatılamadı: " + ex.Message);
                return string.Empty;
            }
        }

        public static string ResizeVideoWithPreset(string inputPath, string outputPath, VideoPreset preset)
        {
            // Eğer outputPath uzantısızsa .mp4 ekle
            if (string.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
                outputPath += ".mp4";

            string filter = preset switch
            {
                VideoPreset.Portrait => "scale='if(gt(a,9/16),1080,-2)':'if(gt(a,9/16),-2,1920)',pad=1080:1920:(1080-iw)/2:(1920-ih)/2",
                VideoPreset.Landscape => "scale='if(gt(a,16/9),1920,-2)':'if(gt(a,16/9),-2,1080)',pad=1920:1080:(1920-iw)/2:(1080-ih)/2",
                VideoPreset.Square => "scale='if(gt(a,1),1080,-2)':'if(gt(a,1),-2,1080)',pad=1080:1080:(1080-iw)/2:(1080-ih)/2",
                _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null)
            };

            var psi = new ProcessStartInfo
            {
                FileName = GeneralSettings.GetFFmpegPath(),
                Arguments = $"-y -i \"{inputPath}\" -vf \"{filter}\" -c:a copy \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string errorLog = "";

            using var process = new Process { StartInfo = psi };
            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    Console.WriteLine("FFmpeg: " + e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    errorLog += e.Data + "\n";
                    Console.Error.WriteLine("FFmpeg HATA: " + e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (File.Exists(outputPath))
            {
                Console.WriteLine($"✅ Video '{preset}' çözünürlüğüne dönüştürüldü: {outputPath}");
                return outputPath;
            }
            else
            {
                Console.Error.WriteLine("❌ FFmpeg çıktıyı oluşturamadı.");
                Console.Error.WriteLine(errorLog);
                return string.Empty;
            }
        }

        public static void CropToVertical(string inputPath, string outputPath, int faceCenterX, int width, int height)
        {
            string ffmpegPath = GeneralSettings.GetFFmpegPath();

            int verticalHeight = height;
            int verticalWidth = (int)(verticalHeight * 9.0 / 16.0);

            // Yüzün merkezine göre kırpma başlangıç noktası hesapla
            int cropX = Math.Max(0, faceCenterX - verticalWidth / 2);
            if (cropX + verticalWidth > width)
                cropX = width - verticalWidth;

            string args = $"-i \"{inputPath}\" -vf \"crop={verticalWidth}:{verticalHeight}:{cropX}:0\" -c:a copy \"{outputPath}\"";

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
            process.WaitForExit();
            Console.WriteLine("Video kırpma tamamlandı.");
        }


    }
}

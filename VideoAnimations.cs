using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RoboTube
{
    public class VideoAnimations
    {
        public static void AddScrollingTextToVideo(
    string inputVideo,
    string outputVideo,
    string text,
    string fontFile,
    int fontSize = 24,
    string fontColor = "white",
    int yPosition = 50,
    int speed = 100
)
        {
            if (fontFile == null || !System.IO.File.Exists(fontFile))
            {
                fontFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "font.ttf");
            }
            // FFmpeg drawtext filtresi
            string drawText = $"drawtext=fontfile='{fontFile}':" +
                          $"text='{text}':" +
                          $"fontcolor={fontColor}:" +
                          $"fontsize={fontSize}:" +
                          $"x='w-mod(t*{speed}, w+tw)':" +
                          $"y={yPosition}";

            // FFmpeg komutu
            string arguments = $"-y -i \"{inputVideo}\" -vf \"{drawText}\" -codec:a copy \"{outputVideo}\"";
            var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "ffmpeg");

            // FFmpeg process'i başlat
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
        }
    }
}
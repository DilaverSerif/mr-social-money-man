using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoboTube
{
    public static class GeneralSettings
    {
        private static readonly string DateTimeNow = DateTime.Now.ToString("yyyy-MM-dd");
        public static string GetFFmpegPath()
        {
            // FFmpeg'in çalıştırılabilir dosyasının yolu
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "ffmpeg");
        }
        public static string GetDownloadDirectory(string videoName)
        {
            // Varsayılan indirme dizini, uygulamanın çalıştığı dizin altında "downloads" klasörü
            string downloadDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads");
            string dateTimeFolderDirectory = Path.Combine(downloadDirectory, DateTimeNow);
            string videoNameFolderDirectory = Path.Combine(dateTimeFolderDirectory, videoName);
            
            // Eğer "downloads" klasörü yoksa oluştur
            if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
            }

            // Eğer tarih klasörü yoksa oluştur
            if (!Directory.Exists(dateTimeFolderDirectory))
            {
                Directory.CreateDirectory(dateTimeFolderDirectory);
            }

            // Eğer video ismi klasörü yoksa oluştur
            if (!Directory.Exists(videoNameFolderDirectory))
            {
                Directory.CreateDirectory(videoNameFolderDirectory);
            }

            return videoNameFolderDirectory;
        }

        public static string GetOutputDirectory(string getVideoTitle)
        {
            // Varsayılan çıktı dizini, uygulamanın çalıştığı dizin altında "output" klasörü
            string downloadDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads");
            string dateTimeFolderDirectory = Path.Combine(downloadDirectory, DateTimeNow);
            string videoTitleDirectory = Path.Combine(dateTimeFolderDirectory, getVideoTitle);
            string outputDirectory = Path.Combine(videoTitleDirectory, "output");

            // Eğer "output" klasörü yoksa oluştur
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            return outputDirectory;
        }

        public static string GetWavExportPath(string getVideoTitle)
        {
            string downloadDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads");
            string dateTimeFolderDirectory = Path.Combine(downloadDirectory, DateTimeNow);
            string videoTitleDirectory = Path.Combine(dateTimeFolderDirectory, getVideoTitle);
            string outputDirectory = Path.Combine(videoTitleDirectory, "output");
            string wavExportPath = Path.Combine(outputDirectory, "audio");

            // Eğer "audio" klasörü yoksa oluştur
            if (!Directory.Exists(wavExportPath))
            {
                Directory.CreateDirectory(wavExportPath);
            }
            
            return wavExportPath;
        }

        public static string GetOutputDirectoryForJson(string getVideoTitle)
        {
            string jsonDirectory = Path.Combine(GetOutputDirectory(getVideoTitle), "json");
            
            // Eğer "json" klasörü yoksa oluştur
            if (!Directory.Exists(jsonDirectory))
            {
                Directory.CreateDirectory(jsonDirectory);
            }
            
            return jsonDirectory;
        }
    }
}
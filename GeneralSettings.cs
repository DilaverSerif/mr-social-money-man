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

        public static string GetVideoDirectory(string videoTitle)
        {
            var downloadDirectory = GetDownloadDirectory(videoTitle);
            var videoDirectory = Path.Combine(downloadDirectory, $"{videoTitle}.mp4");

            return videoDirectory;
        }
        
        public static string GetTrimVideoDirectory(string videoTitle)
        {
            var downloadDirectory = GetDownloadDirectory(videoTitle);
            var videoDirectory = Path.Combine(downloadDirectory, $"{videoTitle}_trimmed.mp4");

            return videoDirectory;
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

        // public static string GetOutputDirectory(string getVideoTitle)
        // {
        //     // Varsayılan çıktı dizini, uygulamanın çalıştığı dizin altında "output" klasörü
        //     string downloadDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads");
        //     string dateTimeFolderDirectory = Path.Combine(downloadDirectory, DateTimeNow);
        //     string videoTitleDirectory = Path.Combine(dateTimeFolderDirectory, getVideoTitle);
        //     string outputDirectory = Path.Combine(videoTitleDirectory, "output");
        //
        //     // Eğer "output" klasörü yoksa oluştur
        //     if (!Directory.Exists(outputDirectory))
        //     {
        //         Directory.CreateDirectory(outputDirectory);
        //     }
        //
        //     return outputDirectory;
        // }
        
        public static string GetWavByOutputPath(string getVideoTitle)
        {
            string downloadDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads");
            string dateTimeFolderDirectory = Path.Combine(downloadDirectory, DateTimeNow);
            string videoTitleDirectory = Path.Combine(dateTimeFolderDirectory, getVideoTitle);
            // string outputDirectory = Path.Combine(videoTitleDirectory, "output");
            // string wavExportPath = Path.Combine(outputDirectory, "audio");
            string wavExportPath = Path.Combine(videoTitleDirectory, "audio.wav");

            // // Eğer "audio" klasörü yoksa oluştur
            // if (!Directory.Exists(wavExportPath))
            // {
            //     Directory.CreateDirectory(wavExportPath);
            // }
            
            return wavExportPath;
        }
        
        public static string SetWayToWavByOutputPath(string getVideoTitle,string filename)
        {
            string outputDirectory = GetDownloadDirectory(getVideoTitle);
            string wavExportPath = Path.Combine(outputDirectory, filename + ".wav");

            // Eğer WAV çıktısı dosyası yoksa oluştur
            if (!File.Exists(wavExportPath))
            {
                File.Create(wavExportPath).Close();
            }

            return wavExportPath;
        }
        
        
        
        public static string SetJsonByOutputPath(string getVideoTitle,string filename)
        {
            string outputDirectory = GetDownloadDirectory(getVideoTitle);
            string jsonOutputPath = Path.Combine(outputDirectory, filename+ ".json");

            // Eğer JSON çıktısı dosyası yoksa oluştur
            if (!File.Exists(jsonOutputPath))
            {
                File.Create(jsonOutputPath).Close();
            }

            return jsonOutputPath;
        }
        
        public static string GetOutputDirectoryForJson(string getVideoTitle,string filename)
        {
            // Varsayılan çıktı dizini, uygulamanın çalıştığı dizin altında "output" klasörü
            string outputDirectory = GetDownloadDirectory(getVideoTitle);
            string jsonOutputPath = Path.Combine(outputDirectory, filename + ".json");
        
            return jsonOutputPath;
        }

        public static string GetTYTDLPPath()
        {
           return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "yt-dlp");
        }

        public static string GetTranscriptByVideoTitle(string videoTitle)
        {
            string outputDirectory = GetDownloadDirectory(videoTitle);
            string jsonOutputPath = Path.Combine(outputDirectory, "transcript.json");
            
            return jsonOutputPath;
        }

        public static string GetFaceCropVideoDirectory(string videoTitle, FaceSelectionStrategy strategy)
        {
            string outputDirectory = GetDownloadDirectory(videoTitle);
            string faceCropVideoPath = Path.Combine(outputDirectory, $"{videoTitle}_facecrop_{strategy}.mp4");
            
            return faceCropVideoPath;
        }
    }
}
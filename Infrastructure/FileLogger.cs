using System;
using System.IO;
using ChronoDesk.Core;

namespace ChronoDesk.Infrastructure
{
    public class FileLogger : ILogger
    {
        private readonly string _logFolder;

        public FileLogger()
        {
            _logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChronoDesk", "Logs");
            Directory.CreateDirectory(_logFolder);
        }

        public void LogInfo(string message) => WriteLog("INFO", message);
        
        public void LogError(string message, Exception? ex = null) => WriteLog("ERROR", ex == null ? message : $"{message} | Exception: {ex.Message}\n{ex.StackTrace}");

        private void WriteLog(string type, string message)
        {
            try
            {
                string fileName = $"log_{DateTime.Now:yyyy-MM-dd}.txt";
                string path = Path.Combine(_logFolder, fileName);
                string logEntry = $"[{DateTime.Now:HH:mm:ss}] [{type}] {message}{Environment.NewLine}";
                
                File.AppendAllText(path, logEntry);
            }
            catch { /* جلوگیری از کرش در صورت مشکل دسترسی فایل */ }
        }
    }
}

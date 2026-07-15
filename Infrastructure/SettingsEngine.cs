using System;
using System.IO;
using System.Text.Json;
using ChronoDesk.Core;

namespace ChronoDesk.Infrastructure
{
    public class SettingsEngine : ISettingsEngine
    {
        private static readonly string AppFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChronoDesk");
        
        private static readonly string FilePath = Path.Combine(AppFolder, "settings.json");

        public AppSettings Settings { get; private set; } = new AppSettings();

        public void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception)
            {
                Settings = new AppSettings();
                try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(AppFolder);
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch (Exception) { }
        }
    }
}

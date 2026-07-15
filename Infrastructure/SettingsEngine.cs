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

        public WidgetSettings Settings { get; private set; } = new WidgetSettings();

        public void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    // دی‌سریالایز کردن تنظیمات
                    Settings = JsonSerializer.Deserialize<WidgetSettings>(json) ?? new WidgetSettings();
                }
            }
            catch (Exception)
            {
                // مکانیزم Self-Healing: در صورت خرابی فایل، تنظیمات پیش‌فرض بارگذاری می‌شود
                Settings = new WidgetSettings();
                
                // پاک کردن فایل خراب برای جلوگیری از خطاهای بعدی
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
            catch (Exception)
            {
                // جلوگیری از کرش در صورت مشکل دسترسی به دیسک (مثلاً آنتی‌ویروس)
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;

namespace ChronoDesk.Infrastructure
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private Dictionary<string, string> _translations = new();
        private string _currentLanguage = "en";

        public string CurrentLanguage => _currentLanguage;
        
        // جهت متن (راست‌چین برای فارسی، چپ‌چین برای انگلیسی)
        public FlowDirection FlowDirection => _currentLanguage == "fa" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        // ایندکسر برای دسترسی به ترجمه‌ها در XAML
        public string this[string key]
        {
            get
            {
                if (_translations.TryGetValue(key, out var value))
                    return value;
                return key; // اگر کلید پیدا نشد، خود کلید را برمی‌گرداند
            }
        }

        public void LoadLanguage(string langCode)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", $"{langCode}.json");
                if (!File.Exists(path))
                {
                    langCode = "en"; // Fallback to English
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", "en.json");
                }

                var json = File.ReadAllText(path);
                _translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                _currentLanguage = langCode;
                
                // اطلاع رسانی به UI برای آپدیت تمام متن‌ها و جهت صفحه
                OnPropertyChanged("Item");
                OnPropertyChanged(nameof(FlowDirection));
            }
            catch { }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

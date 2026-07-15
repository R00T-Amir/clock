using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ChronoDesk.Infrastructure
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private Dictionary<string, string> _translations = new();
        private string _currentLanguage = "en";

        public string CurrentLanguage => _currentLanguage;
        
        // رفع تداخل: مشخص کردن دقیق WPF FlowDirection
        public System.Windows.FlowDirection FlowDirection => _currentLanguage == "fa" ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;

        public string this[string key]
        {
            get
            {
                if (_translations.TryGetValue(key, out var value))
                    return value;
                return key;
            }
        }

        public void LoadLanguage(string langCode)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", $"{langCode}.json");
                if (!File.Exists(path))
                {
                    langCode = "en";
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", "en.json");
                }

                var json = File.ReadAllText(path);
                _translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                _currentLanguage = langCode;
                
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

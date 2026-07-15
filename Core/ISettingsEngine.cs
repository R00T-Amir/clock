using System;
using System.Collections.Generic;

namespace ChronoDesk.Core
{
    public interface ISettingsEngine
    {
        AppSettings Settings { get; }
        void Load();
        void Save();
    }

    public class WidgetConfig
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CityName { get; set; } = "Local";
        public string TimeZoneId { get; set; } = TimeZoneInfo.Local.Id;
        public double Left { get; set; } = 100;
        public double Top { get; set; } = 100;
        public string BackgroundColor { get; set; } = "#CC1F1F1F";
        public string TextColor { get; set; } = "#FFFFFF";
        public string DateColor { get; set; } = "#99FFFFFF";
        public double Opacity { get; set; } = 1.0;
    }

    public class AppSettings
    {
        public List<WidgetConfig> Widgets { get; set; } = new();
    }
}

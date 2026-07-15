namespace ChronoDesk.Core
{
    public interface ISettingsEngine
    {
        WidgetSettings Settings { get; }
        void Load();
        void Save();
    }

    public class WidgetSettings
    {
        public double Left { get; set; } = 100;
        public double Top { get; set; } = 100;
        
        // تنظیمات شخصی‌سازی
        public string BackgroundColor { get; set; } = "#CC1F1F1F";
        public string TextColor { get; set; } = "#FFFFFF";
        public string DateColor { get; set; } = "#99FFFFFF";
        public double Opacity { get; set; } = 1.0;
    }
}

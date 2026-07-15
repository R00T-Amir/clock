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
    }
}

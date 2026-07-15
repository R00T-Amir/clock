namespace ChronoDesk.Core
{
    public interface IWidgetManager
    {
        void Initialize();
        void CreateWidget(string cityName, string timeZoneId);
        void SaveAll();
        void Shutdown();
    }
}

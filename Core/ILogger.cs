namespace ChronoDesk.Core
{
    public interface ILogger
    {
        void LogInfo(string message);
        void LogError(string message, System.Exception? ex = null);
    }
}

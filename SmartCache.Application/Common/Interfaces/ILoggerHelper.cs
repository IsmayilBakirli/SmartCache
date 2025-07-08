namespace SmartCache.Application.Common.Interfaces
{
    public interface ILoggerHelper
    {
        void LogInfo(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, params object[] args);
    }
}

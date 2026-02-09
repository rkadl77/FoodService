
namespace hitsApplication.Services.Interfaces
{
    public interface IBugCaseLoggingService
    {
        void LogBackendRequest(string method, string endpoint, int httpStatus, string userId = null);
        Task LogBackendRequestAsync(string method, string endpoint, int httpStatus, string userId = null);
        void ChangeLogPath(string newPath);
        void RotateLogFileIfNeeded();
    }
}
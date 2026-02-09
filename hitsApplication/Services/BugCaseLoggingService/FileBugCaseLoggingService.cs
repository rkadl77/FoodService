using System;
using System.IO;
using System.Threading.Tasks;
using hitsApplication.Models;
using hitsApplication.Services.Interfaces; 
using Microsoft.Extensions.Options;

namespace hitsApplication.Services 
{
    public class FileBugCaseLoggingService : IBugCaseLoggingService 
    {
        private readonly BugCaseLoggingSettings _settings;
        private readonly object _lock = new object();

        public FileBugCaseLoggingService(IOptions<BugCaseLoggingSettings> settings)
        {
            _settings = settings.Value;
            EnsureLogDirectoryExists();
            CleanOldLogs();
        }

        public void LogBackendRequest(string method, string endpoint, int httpStatus, string userId = null)
        {
            if (!_settings.EnableLogging)
                return;

            try
            {
                RotateLogFileIfNeeded();

                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                var logEntry = $"{timestamp} {_settings.ServiceName} {method} {endpoint} {httpStatus} {userId ?? "anonymous"}";

                lock (_lock)
                {
                    File.AppendAllText(_settings.LogFilePath, logEntry + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BUG-CASE-LOG-FAIL] {ex.Message}");
            }
        }

        public async Task LogBackendRequestAsync(string method, string endpoint, int httpStatus, string userId = null)
        {
            if (!_settings.EnableLogging)
                return;

            await Task.Run(() => LogBackendRequest(method, endpoint, httpStatus, userId));
        }

        public void ChangeLogPath(string newPath)
        {
            lock (_lock)
            {
                _settings.LogFilePath = newPath;
                EnsureLogDirectoryExists();
            }
        }

        public void RotateLogFileIfNeeded()
        {
            try
            {
                if (!File.Exists(_settings.LogFilePath))
                    return;

                var fileInfo = new FileInfo(_settings.LogFilePath);
                var maxSizeBytes = _settings.MaxFileSizeMB * 1024 * 1024;

                if (fileInfo.Length >= maxSizeBytes)
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var rotatedFilePath = $"{_settings.LogFilePath}.{timestamp}.backup";

                    lock (_lock)
                    {
                        File.Move(_settings.LogFilePath, rotatedFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOG-ROTATION-ERROR] {ex.Message}");
            }
        }

        private void EnsureLogDirectoryExists()
        {
            try
            {
                var directory = Path.GetDirectoryName(_settings.LogFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DIR-CREATE-ERROR] {ex.Message}");
            }
        }

        private void CleanOldLogs()
        {
            try
            {
                var logDirectory = Path.GetDirectoryName(_settings.LogFilePath);
                if (string.IsNullOrEmpty(logDirectory) || !Directory.Exists(logDirectory))
                    return;

                var backupFiles = Directory.GetFiles(logDirectory, "*.backup");
                var cutoffDate = DateTime.Now.AddDays(-_settings.RetainDays);

                foreach (var file in backupFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLEAN-LOGS-ERROR] {ex.Message}");
            }
        }
    }
}
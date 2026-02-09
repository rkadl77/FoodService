using hitsApplication.Models;
using hitsApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text;

namespace hitsApplication.Controllers
{
    [ApiController]
    [Route("api/admin/logging")]
    public class LoggingController : ControllerBase
    {
        private readonly IBugCaseLoggingService _bugCaseLogger;
        private readonly IOptions<BugCaseLoggingSettings> _loggingSettings;
        private readonly ILogger<LoggingController> _logger;

        public LoggingController(
            IBugCaseLoggingService bugCaseLogger,
            IOptions<BugCaseLoggingSettings> loggingSettings,
            ILogger<LoggingController> logger)
        {
            _bugCaseLogger = bugCaseLogger;
            _loggingSettings = loggingSettings;
            _logger = logger;
        }

        [HttpGet("settings")]
        [ProducesResponseType(200)]
        public IActionResult GetLoggingSettings()
        {
            try
            {
                var logFilePath = _loggingSettings.Value.LogFilePath;
                var fileExists = System.IO.File.Exists(logFilePath);
                FileInfo fileInfo = null;

                if (fileExists)
                {
                    fileInfo = new FileInfo(logFilePath);
                }

                var settings = new
                {
                    LogFilePath = logFilePath,
                    ServiceName = _loggingSettings.Value.ServiceName,
                    EnableLogging = _loggingSettings.Value.EnableLogging,
                    LogLevel = _loggingSettings.Value.LogLevel,
                    MaxFileSizeMB = _loggingSettings.Value.MaxFileSizeMB,
                    RetainDays = _loggingSettings.Value.RetainDays,
                    CurrentTime = DateTime.UtcNow,
                    FileExists = fileExists,
                    FileSizeBytes = fileInfo?.Length,
                    FileSizeMB = fileInfo != null ? (fileInfo.Length / 1024.0 / 1024.0).ToString("F2") + " MB" : "N/A",
                    LastModified = fileInfo?.LastWriteTimeUtc
                };

                _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/settings", 200, GetUserId());

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logging settings");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("change-path")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult ChangeLogPath([FromBody] ChangeLogPathRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.NewPath))
                {
                    _bugCaseLogger.LogBackendRequest("POST", "/api/admin/logging/change-path", 400, GetUserId());
                    return BadRequest(new { Success = false, Message = "Path cannot be empty" });
                }

                if (Path.GetInvalidPathChars().Any(request.NewPath.Contains))
                {
                    _bugCaseLogger.LogBackendRequest("POST", "/api/admin/logging/change-path", 400, GetUserId());
                    return BadRequest(new { Success = false, Message = "Invalid path characters" });
                }

                var oldPath = _loggingSettings.Value.LogFilePath;
                _bugCaseLogger.ChangeLogPath(request.NewPath);

                _bugCaseLogger.LogBackendRequest("POST", "/api/admin/logging/change-path", 200, GetUserId());

                return Ok(new
                {
                    Success = true,
                    Message = $"Log path changed successfully",
                    OldPath = oldPath,
                    NewPath = request.NewPath,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing log path to {NewPath}", request.NewPath);
                _bugCaseLogger.LogBackendRequest("POST", "/api/admin/logging/change-path", 500, GetUserId());
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("logs")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult GetLogs([FromQuery] int lines = 100, [FromQuery] bool reverse = true)
        {
            try
            {
                var logFilePath = _loggingSettings.Value.LogFilePath;

                if (!System.IO.File.Exists(logFilePath))
                {
                    _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/logs", 404, GetUserId());
                    return NotFound(new
                    {
                        Success = false,
                        Message = $"Log file not found: {logFilePath}",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var logLines = ReadLastLines(logFilePath, lines, reverse);
                var fileInfo = new FileInfo(logFilePath);

                _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/logs", 200, GetUserId());

                return Ok(new
                {
                    Success = true,
                    FilePath = logFilePath,
                    TotalLines = logLines.Count,
                    RequestedLines = lines,
                    ReverseOrder = reverse,
                    Lines = logLines,
                    Timestamp = DateTime.UtcNow,
                    FileSizeBytes = fileInfo.Length,
                    FileSizeMB = (fileInfo.Length / 1024.0 / 1024.0).ToString("F2") + " MB",
                    LastModified = fileInfo.LastWriteTimeUtc
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading logs");
                _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/logs", 500, GetUserId());
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("search")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult SearchLogs(
            [FromQuery] string searchTerm,
            [FromQuery] int maxResults = 50)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/search", 400, GetUserId());
                    return BadRequest(new { Error = "Search term is required" });
                }

                var logFilePath = _loggingSettings.Value.LogFilePath;

                if (!System.IO.File.Exists(logFilePath))
                {
                    _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/search", 404, GetUserId());
                    return NotFound(new { Error = "Log file not found" });
                }

                var searchResults = SearchInLogs(logFilePath, searchTerm, maxResults);

                _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/search", 200, GetUserId());

                return Ok(new
                {
                    Success = true,
                    SearchTerm = searchTerm,
                    ResultsCount = searchResults.Count,
                    MaxResults = maxResults,
                    Results = searchResults,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching logs for term: {SearchTerm}", searchTerm);
                _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/search", 500, GetUserId());
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("toggle/{enable:bool}")]
        [ProducesResponseType(200)]
        public IActionResult ToggleLogging(bool enable)
        {
            try
            {
                var currentStatus = _loggingSettings.Value.EnableLogging;
                var message = $"Logging is currently {(currentStatus ? "enabled" : "disabled")}. " +
                             $"Requested to {(enable ? "enable" : "disable")}.";

                _bugCaseLogger.LogBackendRequest("POST", $"/api/admin/logging/toggle/{enable}", 200, GetUserId());

                return Ok(new
                {
                    Success = true,
                    Message = message,
                    RequestedState = enable,
                    CurrentState = currentStatus,
                    Note = "To actually change the setting, update appsettings.json and restart the application",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling logging to {Enable}", enable);
                _bugCaseLogger.LogBackendRequest("POST", $"/api/admin/logging/toggle/{enable}", 500, GetUserId());
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("rotate")]
        [ProducesResponseType(200)]
        public IActionResult RotateLogs()
        {
            try
            {
                _bugCaseLogger.RotateLogFileIfNeeded();

                var logFilePath = _loggingSettings.Value.LogFilePath;
                var fileInfo = new FileInfo(logFilePath);
                var backupFiles = GetBackupFiles();

                _bugCaseLogger.LogBackendRequest("POST", "/api/admin/logging/rotate", 200, GetUserId());

                return Ok(new
                {
                    Success = true,
                    Message = "Log rotation completed",
                    FilePath = logFilePath,
                    FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
                    FileSizeMB = fileInfo.Exists ? (fileInfo.Length / 1024.0 / 1024.0).ToString("F2") + " MB" : "0 MB",
                    RotationTime = DateTime.UtcNow,
                    BackupFilesCount = backupFiles.Count,
                    BackupFiles = backupFiles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rotating logs");
                _bugCaseLogger.LogBackendRequest("POST", "/api/admin/logging/rotate", 500, GetUserId());
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("cleanup")]
        [ProducesResponseType(200)]
        public IActionResult CleanupOldLogs()
        {
            try
            {
                var logDirectory = Path.GetDirectoryName(_loggingSettings.Value.LogFilePath);
                var deletedFiles = new List<string>();

                if (!string.IsNullOrEmpty(logDirectory) && Directory.Exists(logDirectory))
                {
                    var backupFiles = Directory.GetFiles(logDirectory, "*.backup");
                    var cutoffDate = DateTime.Now.AddDays(-_loggingSettings.Value.RetainDays);

                    foreach (var file in backupFiles)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffDate)
                        {
                            System.IO.File.Delete(file);
                            deletedFiles.Add(file);
                        }
                    }
                }

                _bugCaseLogger.LogBackendRequest("POST", "/api/admin/logging/cleanup", 200, GetUserId());

                return Ok(new
                {
                    Success = true,
                    Message = $"Cleaned up {deletedFiles.Count} old backup files",
                    DeletedFiles = deletedFiles,
                    RetainDays = _loggingSettings.Value.RetainDays,
                    CleanupTime = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old logs");
                _bugCaseLogger.LogBackendRequest("POST", "/api/admin/logging/cleanup", 500, GetUserId());
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("stats")]
        [ProducesResponseType(200)]
        public IActionResult GetLogStats([FromQuery] string timeframe = "24h")
        {
            try
            {
                var logFilePath = _loggingSettings.Value.LogFilePath;

                if (!System.IO.File.Exists(logFilePath))
                {
                    _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/stats", 200, GetUserId());
                    return Ok(new
                    {
                        Success = true,
                        Message = "Log file does not exist yet",
                        FileExists = false,
                        Timeframe = timeframe,
                        Timestamp = DateTime.UtcNow
                    });
                }

                var stats = CalculateLogStats(logFilePath, timeframe);

                _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/stats", 200, GetUserId());

                return Ok(new
                {
                    Success = true,
                    FilePath = logFilePath,
                    Timeframe = timeframe,
                    Stats = stats,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting log stats for timeframe: {Timeframe}", timeframe);
                _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/stats", 500, GetUserId());
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("test")]
        [AllowAnonymous]
        public IActionResult TestLogging([FromQuery] int count = 5)
        {
            try
            {
                var testResults = new List<string>();
                var userId = GetUserId() ?? "testUser";

                for (int i = 1; i <= count; i++)
                {
                    var testMessage = $"Test log entry #{i} at {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}";
                    _bugCaseLogger.LogBackendRequest("POST", $"/api/admin/logging/test/{i}", 200, userId);
                    testResults.Add(testMessage);
                }

                return Ok(new
                {
                    Success = true,
                    Message = $"Created {count} test log entries",
                    UserId = userId,
                    TestResults = testResults,
                    LogFilePath = _loggingSettings.Value.LogFilePath,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test logging");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("content")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult GetLogContent([FromQuery] int maxLines = 1000)
        {
            try
            {
                var logFilePath = _loggingSettings.Value.LogFilePath;

                if (!System.IO.File.Exists(logFilePath))
                {
                    _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/content", 404, GetUserId());
                    return NotFound(new
                    {
                        Success = false,
                        Error = "Log file not found",
                        FilePath = logFilePath
                    });
                }

                var lines = System.IO.File.ReadAllLines(logFilePath)
                    .Take(maxLines)
                    .ToList();

                _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/content", 200, GetUserId());

                return Ok(new
                {
                    Success = true,
                    FilePath = logFilePath,
                    TotalLinesRead = lines.Count,
                    MaxLines = maxLines,
                    Content = lines,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting log content");
                _bugCaseLogger.LogBackendRequest("GET", "/api/admin/logging/content", 500, GetUserId());
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        #region Вспомогательные методы

        private string GetUserId()
        {
            return User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                   ?? User?.FindFirst("sub")?.Value
                   ?? HttpContext.Items["UserId"]?.ToString()
                   ?? "anonymous";
        }

        private List<string> ReadLastLines(string filePath, int lines, bool reverse)
        {
            var result = new List<string>();

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                var lineCount = 0;
                var buffer = new List<string>();

                while (!streamReader.EndOfStream)
                {
                    buffer.Add(streamReader.ReadLine());
                    lineCount++;

                    if (buffer.Count > lines)
                    {
                        buffer.RemoveAt(0);
                    }
                }

                result = reverse ? buffer : buffer.AsEnumerable().Reverse().ToList();
            }

            return result;
        }

        private List<string> SearchInLogs(string filePath, string searchTerm, int maxResults)
        {
            var results = new List<string>();
            var searchTermLower = searchTerm.ToLower();

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                while (!streamReader.EndOfStream && results.Count < maxResults)
                {
                    var line = streamReader.ReadLine();
                    if (line?.ToLower().Contains(searchTermLower) == true)
                    {
                        results.Add(line);
                    }
                }
            }

            return results;
        }

        private List<string> GetBackupFiles()
        {
            var logDirectory = Path.GetDirectoryName(_loggingSettings.Value.LogFilePath);
            if (string.IsNullOrEmpty(logDirectory) || !Directory.Exists(logDirectory))
                return new List<string>();

            return Directory.GetFiles(logDirectory, "*.backup")
                .Select(f => new
                {
                    Name = Path.GetFileName(f),
                    Size = new FileInfo(f).Length,
                    Created = System.IO.File.GetCreationTime(f)
                })
                .OrderByDescending(f => f.Created)
                .Select(f => $"{f.Name} ({(f.Size / 1024.0):F2} KB, {f.Created:yyyy-MM-dd HH:mm:ss})")
                .ToList();
        }

        private object CalculateLogStats(string filePath, string timeframe)
        {
            var stats = new Dictionary<string, int>
            {
                ["GET"] = 0,
                ["POST"] = 0,
                ["PUT"] = 0,
                ["DELETE"] = 0,
                ["200"] = 0,
                ["201"] = 0,
                ["400"] = 0,
                ["401"] = 0,
                ["404"] = 0,
                ["500"] = 0
            };

            var timeframeHours = timeframe switch
            {
                "1h" => 1,
                "6h" => 6,
                "24h" => 24,
                "7d" => 168,
                "30d" => 720,
                _ => 24
            };

            var cutoffTime = DateTime.UtcNow.AddHours(-timeframeHours);

            if (System.IO.File.Exists(filePath))
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = streamReader.ReadLine();
                        if (string.IsNullOrEmpty(line))
                            continue;

                        var parts = line.Split(' ', 6);
                        if (parts.Length >= 5)
                        {
                            if (DateTime.TryParse(parts[0], out var logTime) && logTime >= cutoffTime)
                            {
                                var method = parts[2];
                                var status = parts[4];

                                if (stats.ContainsKey(method))
                                    stats[method]++;

                                if (stats.ContainsKey(status))
                                    stats[status]++;
                            }
                        }
                    }
                }
            }

            return new
            {
                TimeframeHours = timeframeHours,
                RequestsByMethod = new
                {
                    GET = stats["GET"],
                    POST = stats["POST"],
                    PUT = stats["PUT"],
                    DELETE = stats["DELETE"]
                },
                ResponsesByStatus = new
                {
                    OK_200 = stats["200"],
                    Created_201 = stats["201"],
                    BadRequest_400 = stats["400"],
                    Unauthorized_401 = stats["401"],
                    NotFound_404 = stats["404"],
                    InternalError_500 = stats["500"]
                },
                TotalRequests = stats["GET"] + stats["POST"] + stats["PUT"] + stats["DELETE"],
                Timeframe = timeframe,
                FromDate = cutoffTime,
                ToDate = DateTime.UtcNow
            };
        }

        #endregion
    }

    public class ChangeLogPathRequest
    {
        public string NewPath { get; set; }
    }
}
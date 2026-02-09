public class BugCaseLoggingSettings
{
    public string LogFilePath { get; set; } = "logs/backend_bugcase.log";
    public string ServiceName { get; set; } = "CartService";
    public bool EnableLogging { get; set; } = true;
    public string LogLevel { get; set; } = "Information";
    public int MaxFileSizeMB { get; set; } = 10;
    public int RetainDays { get; set; } = 30;
}
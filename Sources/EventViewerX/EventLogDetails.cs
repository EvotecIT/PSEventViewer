using System.Diagnostics.Eventing.Reader;

namespace EventViewerX;

public class EventLogDetails {
    public string MachineName { get; set; }
    public string LogName { get; set; }
    public string LogType { get; set; }
    public EventLogIsolation LogIsolation { get; set; }
    public bool IsEnabled { get; set; }
    public bool? IsLogFull { get; set; }
    public long MaximumSizeInBytes { get; set; }
    public string LogFilePath { get; set; }
    public string LogMode { get; set; }
    public string OwningProviderName { get; set; }
    public List<string> ProviderNames { get; set; }
    public string ProviderLevel { get; set; }
    public string ProviderKeywords { get; set; }
    public int ProviderBufferSize { get; set; }
    public int ProviderMinimumNumberOfBuffers { get; set; }
    public int ProviderMaximumNumberOfBuffers { get; set; }
    public int ProviderLatency { get; set; }
    public string ProviderControlGuid { get; set; }
    public DateTime? CreationTime { get; set; }
    public DateTime? LastAccessTime { get; set; }
    public DateTime? LastWriteTime { get; set; }
    public long? FileSize { get; set; }
    public long? FileSizeMaximum;
    public double? FileSizeCurrentMB;
    public double? FileSizeMaximumMB;
    public long? RecordCount { get; set; }
    public long? OldestRecordNumber { get; set; }
    public string SecurityDescriptor { get; set; }
    public bool IsClassicLog { get; set; }

    public DateTime? NewestEvent;
    public DateTime? OldestEvent;
    public int? Attributes { get; set; }

    public EventLogDetails(InternalLogger internalLogger, string machineName, EventLogConfiguration logConfig, EventLogInformation logInfoObj) {
        LogName = logConfig.LogName;
        LogType = logConfig.LogType.ToString();
        IsEnabled = logConfig.IsEnabled;
        MaximumSizeInBytes = logConfig.MaximumSizeInBytes;
        LogFilePath = logConfig.LogFilePath;
        LogIsolation = logConfig.LogIsolation;
        LogMode = logConfig.LogMode.ToString();
        OwningProviderName = logConfig.OwningProviderName;
        try {
            ProviderNames = new List<string>(logConfig.ProviderNames);
        } catch (Exception ex) {
            internalLogger.WriteWarning("Couldn't get provider names for " + LogName + ". Error: " + ex.Message);
            ProviderNames = new List<string>();
        }
        ProviderBufferSize = logConfig.ProviderBufferSize.GetValueOrDefault();
        ProviderMinimumNumberOfBuffers = logConfig.ProviderMinimumNumberOfBuffers.GetValueOrDefault();
        ProviderMaximumNumberOfBuffers = logConfig.ProviderMaximumNumberOfBuffers.GetValueOrDefault();
        ProviderLatency = logConfig.ProviderLatency.GetValueOrDefault();
        ProviderControlGuid = logConfig.ProviderControlGuid.ToString();
        SecurityDescriptor = logConfig.SecurityDescriptor;
        ProviderLevel = logConfig.ProviderLevel.ToString();
        ProviderKeywords = logConfig.ProviderKeywords.ToString();
        IsClassicLog = logConfig.IsClassicLog;

        if (logInfoObj != null) {
            FileSize = logInfoObj.FileSize;
            RecordCount = logInfoObj.RecordCount;
            OldestRecordNumber = logInfoObj.OldestRecordNumber;
            LastAccessTime = logInfoObj.LastAccessTime;
            LastWriteTime = logInfoObj.LastWriteTime;
            CreationTime = logInfoObj.CreationTime;

            FileSizeCurrentMB = ConvertSize(FileSize, "B", "MB", 2);
            IsLogFull = logInfoObj.IsLogFull;
            Attributes = logInfoObj.Attributes;
        }

        FileSizeMaximum = logConfig.MaximumSizeInBytes;
        FileSizeMaximumMB = ConvertSize(FileSizeMaximum, "B", "MB", 2);
        MachineName = machineName;
    }

    private static double ConvertSize(double? value, string fromUnit, string toUnit, int precision) {
        if (!value.HasValue) {
            return 0;
        }

        double size = (double)value.Value;

        switch (fromUnit.ToUpper()) {
            case "B":
                break;
            case "KB":
                size *= 1024.0;
                break;
            case "MB":
                size *= 1024.0 * 1024.0;
                break;
            case "GB":
                size *= 1024.0 * 1024.0 * 1024.0;
                break;
            case "TB":
                size *= 1024.0 * 1024.0 * 1024.0 * 1024.0;
                break;
        }

        switch (toUnit.ToUpper()) {
            case "B":
                break;
            case "KB":
                size /= 1024.0;
                break;
            case "MB":
                size /= 1024.0 * 1024.0;
                break;
            case "GB":
                size /= 1024.0 * 1024.0 * 1024.0;
                break;
            case "TB":
                size /= 1024.0 * 1024.0 * 1024.0 * 1024.0;
                break;
        }

        return Math.Round(size, precision);
    }
}
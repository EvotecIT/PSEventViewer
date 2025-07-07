using System.Diagnostics.Eventing.Reader;

namespace EventViewerX;

/// <summary>
/// Describes configuration and status of a single event log.
/// </summary>
public class EventLogDetails {
    /// <summary>Machine that hosts the log.</summary>
    public string MachineName { get; set; }
    /// <summary>Name of the log.</summary>
    public string LogName { get; set; }
    /// <summary>Type of the log.</summary>
    public string LogType { get; set; }
    /// <summary>Log isolation mode.</summary>
    public EventLogIsolation LogIsolation { get; set; }
    /// <summary>Indicates whether the log is enabled.</summary>
    public bool IsEnabled { get; set; }
    /// <summary>Indicates whether the log file reached its maximum size.</summary>
    public bool? IsLogFull { get; set; }
    /// <summary>Maximum configured size in bytes.</summary>
    public long MaximumSizeInBytes { get; set; }
    /// <summary>Path to the physical log file.</summary>
    public string LogFilePath { get; set; }
    /// <summary>Current logging mode.</summary>
    public string LogMode { get; set; }
    /// <summary>Owning provider name.</summary>
    public string OwningProviderName { get; set; }
    /// <summary>List of providers registered for the log.</summary>
    public List<string> ProviderNames { get; set; }
    /// <summary>Provider level mask.</summary>
    public string ProviderLevel { get; set; }
    /// <summary>Provider keywords mask.</summary>
    public string ProviderKeywords { get; set; }
    /// <summary>Buffer size used by the provider.</summary>
    public int ProviderBufferSize { get; set; }
    /// <summary>Minimum number of buffers for the provider.</summary>
    public int ProviderMinimumNumberOfBuffers { get; set; }
    /// <summary>Maximum number of buffers for the provider.</summary>
    public int ProviderMaximumNumberOfBuffers { get; set; }
    /// <summary>Provider latency setting.</summary>
    public int ProviderLatency { get; set; }
    /// <summary>Control GUID for the provider.</summary>
    public string ProviderControlGuid { get; set; }
    /// <summary>Creation time of the log file.</summary>
    public DateTime? CreationTime { get; set; }
    /// <summary>Last access time of the log file.</summary>
    public DateTime? LastAccessTime { get; set; }
    /// <summary>Last write time of the log file.</summary>
    public DateTime? LastWriteTime { get; set; }
    /// <summary>Current file size in bytes.</summary>
    public long? FileSize { get; set; }
    /// <summary>Maximum configured file size in bytes.</summary>
    public long? FileSizeMaximum;
    /// <summary>Current file size in megabytes.</summary>
    public double? FileSizeCurrentMB;
    /// <summary>Maximum file size in megabytes.</summary>
    public double? FileSizeMaximumMB;
    /// <summary>Total number of records.</summary>
    public long? RecordCount { get; set; }
    /// <summary>Oldest record number.</summary>
    public long? OldestRecordNumber { get; set; }
    /// <summary>Security descriptor of the log.</summary>
    public string SecurityDescriptor { get; set; }
    /// <summary>Indicates if the log is classic type.</summary>
    public bool IsClassicLog { get; set; }

    /// <summary>Newest event timestamp.</summary>
    public DateTime? NewestEvent;
    /// <summary>Oldest event timestamp.</summary>
    public DateTime? OldestEvent;
    /// <summary>Additional log attributes.</summary>
    public int? Attributes { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventLogDetails"/> class.
    /// </summary>
    /// <param name="internalLogger">Logger used for warnings.</param>
    /// <param name="machineName">Name of the computer hosting the log.</param>
    /// <param name="logConfig">Event log configuration.</param>
    /// <param name="logInfoObj">Optional log information object.</param>
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

    /// <summary>
    /// Converts a numeric size value between units.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <param name="fromUnit">Current unit of measure.</param>
    /// <param name="toUnit">Destination unit of measure.</param>
    /// <param name="precision">Number of decimal places.</param>
    /// <returns>Converted value.</returns>
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
            default:
                // Treat unknown units as bytes
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
            default:
                // Keep size unchanged for unknown units
                break;
        }

        return Math.Round(size, precision);
    }
}
using System.Diagnostics.Eventing.Reader;

namespace PSEventViewer;

/// <summary>
/// Configures Windows Event Log settings including log mode, maximum size, and event actions.
/// Supports both individual logs and bulk configuration across multiple machines.
/// </summary>
[Cmdlet(VerbsCommon.Set, "EVXInfo")]
[Alias("Set-EventViewerXInfo", "Set-EventsInformation", "Set-EventsSettings")]
[OutputType(typeof(bool))]
public sealed class CmdletSetEVXInfo : AsyncPSCmdlet {
    /// <summary>
    /// Name of the log whose settings will be modified.
    /// </summary>
    [Parameter(Mandatory = true)]
    public string LogName { get; set; } = string.Empty;

    /// <summary>
    /// Target computer on which to modify the log.
    /// </summary>
    [Parameter]
    [Alias("ComputerName", "ServerName")]
    public string? ComputerName { get; set; }

    /// <summary>
    /// Maximum size of the log in megabytes.
    /// </summary>
    [Parameter]
    public int MaximumSizeMB { get; set; }

    /// <summary>
    /// Maximum size of the log in bytes.
    /// </summary>
    [Parameter]
    public long MaximumSizeInBytes { get; set; }

    /// <summary>
    /// Action to take when the log reaches its maximum size.
    /// </summary>
    [Parameter]
    [ValidateSet(
        "OverwriteEventsAsNeededOldestFirst",
        "ArchiveTheLogWhenFullDoNotOverwrite",
        "DoNotOverwriteEventsClearLogManually")]
    public string? EventAction { get; set; }

    /// <summary>
    /// Log mode to apply to the specified log.
    /// </summary>
    [Alias("LogMode")]
    [Parameter]
    public EventLogMode? Mode { get; set; }

    private EventLogConfiguration? _log;

    /// <summary>
    /// Initializes the event log configuration object.
    /// </summary>
    protected override Task BeginProcessingAsync() {
        try {
            EventLogSession session = string.IsNullOrEmpty(ComputerName)
                ? new EventLogSession()
                : new EventLogSession(ComputerName);
            _log = new EventLogConfiguration(LogName, session);
        } catch (Exception ex) {
            WriteWarning($"Set‑EVXInfo - Error occured during reading {LogName} log - {ex.Message}");
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies the new configuration values to the log.
    /// </summary>
    protected override Task ProcessRecordAsync() {
        if (_log == null) {
            return Task.CompletedTask;
        }

        if (MyInvocation.BoundParameters.ContainsKey(nameof(EventAction))) {
            _log.LogMode = EventAction switch {
                "OverwriteEventsAsNeededOldestFirst" => EventLogMode.Circular,
                "ArchiveTheLogWhenFullDoNotOverwrite" => EventLogMode.AutoBackup,
                "DoNotOverwriteEventsClearLogManually" => EventLogMode.Retain,
                _ => _log.LogMode
            };
        }

        if (MyInvocation.BoundParameters.ContainsKey(nameof(Mode)) && Mode.HasValue) {
            _log.LogMode = Mode.Value;
        }

        if (MyInvocation.BoundParameters.ContainsKey(nameof(MaximumSizeMB))) {
            _log.MaximumSizeInBytes = (long)MaximumSizeMB * 1024 * 1024;
        }

        if (MyInvocation.BoundParameters.ContainsKey(nameof(MaximumSizeInBytes))) {
            _log.MaximumSizeInBytes = MaximumSizeInBytes;
        }

        if (ShouldProcess(LogName, "Saving event log settings")) {
            try {
                _log.SaveChanges();
            } catch (Exception ex) {
                WriteWarning($"Set‑EVXInfo - Error occured during saving of changes for {LogName} log - {ex.Message}");
            }
        }

        return Task.CompletedTask;
    }
}

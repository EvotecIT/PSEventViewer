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
    [Parameter(Mandatory = true)]
    public string LogName { get; set; }

    [Parameter]
    [Alias("ComputerName", "ServerName")]
    public string ComputerName { get; set; }

    [Parameter]
    public int MaximumSizeMB { get; set; }

    [Parameter]
    public long MaximumSizeInBytes { get; set; }

    [Parameter]
    [ValidateSet(
        "OverwriteEventsAsNeededOldestFirst",
        "ArchiveTheLogWhenFullDoNotOverwrite",
        "DoNotOverwriteEventsClearLogManually")]
    public string EventAction { get; set; }

    [Alias("LogMode")]
    [Parameter]
    public EventLogMode? Mode { get; set; }

    private EventLogConfiguration _log;

    protected override Task BeginProcessingAsync() {
        try {
            EventLogSession session = string.IsNullOrEmpty(ComputerName)
                ? new EventLogSession()
                : new EventLogSession(ComputerName);
            _log = new EventLogConfiguration(LogName, session);
        } catch (Exception ex) {
            WriteWarning($"Set-WinEventSettings - Error occured during reading {LogName} log - {ex.Message}");
        }
        return Task.CompletedTask;
    }

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
                WriteWarning($"Set-WinEventSettings - Error occured during saving of changes for {LogName} log - {ex.Message}");
            }
        }

        return Task.CompletedTask;
    }
}

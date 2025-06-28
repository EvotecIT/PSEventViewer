namespace PSEventViewer;

using System.Diagnostics;
using System.Management.Automation;
using System.Threading.Tasks;

/// <summary>
/// Removes an event source from Windows Event Log.
/// </summary>
[Cmdlet(VerbsCommon.Remove, "EVXSource", SupportsShouldProcess = true)]
[Alias("Remove-EventViewerXSource", "Remove-WinEventSource")]
[OutputType(typeof(bool))]
public sealed class CmdletRemoveEVXSource : AsyncPSCmdlet {
    [Parameter(Mandatory = true, Position = 0)]
    [Alias("Source", "Provider")]
    public string SourceName { get; set; }

    [Parameter]
    [Alias("ComputerName", "ServerName")]
    public string MachineName { get; set; }

    protected override Task ProcessRecordAsync() {
        var errorAction = GetErrorActionPreference();
        try {
            if (string.IsNullOrEmpty(MachineName)) {
                if (EventLog.SourceExists(SourceName)) {
                    if (ShouldProcess(SourceName, "Delete event source")) {
                        EventLog.DeleteEventSource(SourceName);
                    }
                } else {
                    WriteWarning($"Remove-EVXSource - Source {SourceName} was not found.");
                    WriteObject(false);
                    return Task.CompletedTask;
                }
            } else {
                if (EventLog.SourceExists(SourceName, MachineName)) {
                    if (ShouldProcess($"{SourceName} on {MachineName}", "Delete event source")) {
                        EventLog.DeleteEventSource(SourceName, MachineName);
                    }
                } else {
                    WriteWarning($"Remove-EVXSource - Source {SourceName} was not found on {MachineName}.");
                    WriteObject(false);
                    return Task.CompletedTask;
                }
            }
            WriteObject(true);
        } catch (Exception ex) {
            WriteWarning($"Remove-EVXSource - Error removing source {SourceName}: {ex.Message}");
            if (errorAction == ActionPreference.Stop) {
                ThrowTerminatingError(new ErrorRecord(ex, "RemoveEVXSourceFailed", ErrorCategory.InvalidOperation, SourceName));
            } else {
                WriteObject(false);
            }
        }
        return Task.CompletedTask;
    }
}

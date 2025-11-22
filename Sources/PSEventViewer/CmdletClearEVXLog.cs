using EventViewerX;
using System.Management.Automation;
using System.Threading.Tasks;

namespace PSEventViewer;

/// <summary>
/// Clears entries from an event log using .NET APIs.
/// </summary>
[Cmdlet(VerbsCommon.Clear, "EVXLog", SupportsShouldProcess = true)]
[Alias("Clear-EventViewerXLog", "Clear-WinEventLog")]
[OutputType(typeof(bool))]
public sealed class CmdletClearEVXLog : AsyncPSCmdlet {
    /// <summary>Log name to clear.</summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string LogName { get; set; }

    /// <summary>Target computer.</summary>
    [Alias("ComputerName", "ServerName")]
    [Parameter]
    public string MachineName { get; set; }

    /// <summary>Retention days to set after clearing.</summary>
    [Parameter]
    public int? RetentionDays { get; set; }

    /// <summary>Performs log clearing.</summary>
    protected override Task ProcessRecordAsync() {
        var errorAction = GetErrorActionPreference();
        try {
            if (ShouldProcess($"{LogName} on {MachineName ?? "localhost"}", "Clear event log")) {
                bool result = SearchEvents.ClearLog(LogName, MachineName, RetentionDays);
                WriteObject(result);
            } else {
                WriteObject(false);
            }
        } catch (Exception ex) {
            WriteWarning($"Clear-EVXLog - Error clearing log {LogName}: {ex.Message}");
            if (errorAction == ActionPreference.Stop) {
                ThrowTerminatingError(new ErrorRecord(ex, "ClearEVXLogFailed", ErrorCategory.InvalidOperation, LogName));
            } else {
                WriteObject(false);
            }
        }
        return Task.CompletedTask;
    }
}

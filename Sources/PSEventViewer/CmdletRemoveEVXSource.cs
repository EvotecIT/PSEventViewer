namespace PSEventViewer;

using System.Diagnostics;
using System.Management.Automation;
using System.Threading.Tasks;
using EventViewerX;

/// <summary>
/// Removes an event source from Windows Event Log.
/// </summary>
[Cmdlet(VerbsCommon.Remove, "EVXSource", SupportsShouldProcess = true)]
[Alias("Remove-EventViewerXSource", "Remove-WinEventSource")]
[OutputType(typeof(bool))]
public sealed class CmdletRemoveEVXSource : AsyncPSCmdlet {
    /// <summary>
    /// Name of the event source to remove.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    [Alias("Source", "Provider")]
    public string SourceName { get; set; }

    /// <summary>
    /// Optional log name to scope source checks (avoids probing Security/State). Defaults to Application when specified.
    /// </summary>
    [Parameter]
    public string LogName { get; set; }

    /// <summary>
    /// Target computer where the source resides.
    /// </summary>
    [Parameter]
    [Alias("ComputerName", "ServerName")]
    public string MachineName { get; set; }

    /// <summary>
    /// Removes the specified event source from the system.
    /// </summary>
    protected override Task ProcessRecordAsync() {
        var errorAction = GetErrorActionPreference();
        try {
            string target = string.IsNullOrEmpty(MachineName)
                ? SourceName
                : $"{SourceName} on {MachineName}";

            if (!ShouldProcess(target, "Delete event source")) {
                WriteObject(false);
                return Task.CompletedTask;
            }

            bool removed = SearchEvents.RemoveSource(SourceName, MachineName, LogName);
            if (!removed) {
                WriteWarning(string.IsNullOrEmpty(MachineName)
                    ? $"Remove-EVXSource - Source {SourceName} was not found."
                    : $"Remove-EVXSource - Source {SourceName} was not found on {MachineName}.");
            }

            WriteObject(removed);
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

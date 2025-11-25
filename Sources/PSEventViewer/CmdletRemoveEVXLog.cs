using System.Management.Automation;
using System.Threading.Tasks;
using EventViewerX;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Removes an event log from the system.</para>
/// <para type="description">Supports local or remote removal with ShouldProcess confirmation; useful for cleanup of custom logs.</para>
/// </summary>
/// <example>
///   <summary>Remove local custom log</summary>
///   <code>Remove-EVXLog -LogName MyApp</code>
///   <para>Deletes the MyApp log from the local computer.</para>
/// </example>
/// <example>
///   <summary>Remove log on remote host</summary>
///   <code>Remove-EVXLog -LogName MyApp -MachineName SRV01</code>
///   <para>Deletes the log on SRV01.</para>
/// </example>
/// <example>
///   <summary>Prompt before removal</summary>
///   <code>Remove-EVXLog -LogName MyApp -Confirm</code>
///   <para>Asks for confirmation prior to deletion.</para>
/// </example>
[Cmdlet(VerbsCommon.Remove, "EVXLog", SupportsShouldProcess = true)]
[Alias("Remove-EventViewerXLog", "Remove-WinEventLog")]
[OutputType(typeof(bool))]
public sealed class CmdletRemoveEVXLog : AsyncPSCmdlet {
    /// <summary>
    /// Name of the log to remove.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string LogName { get; set; } = null!;

    /// <summary>
    /// Target machine from which to remove the log.
    /// </summary>
    [Parameter]
    [Alias("ComputerName", "ServerName")]
    public string? MachineName { get; set; }

    /// <summary>
    /// Removes the specified log.
    /// </summary>
    protected override Task ProcessRecordAsync() {
        var errorAction = GetErrorActionPreference();
        try {
            if (ShouldProcess($"{LogName} on {MachineName ?? "localhost"}", "Remove event log")) {
                bool result = SearchEvents.RemoveLog(LogName, MachineName);
                WriteObject(result);
            } else {
                WriteObject(false);
            }
        } catch (Exception ex) {
            WriteWarning($"Remove-EVXLog - Error removing log {LogName}: {ex.Message}");
            if (errorAction == ActionPreference.Stop) {
                ThrowTerminatingError(new ErrorRecord(ex, "RemoveEVXLogFailed", ErrorCategory.InvalidOperation, LogName));
            } else {
                WriteObject(false);
            }
        }
        return Task.CompletedTask;
    }
}

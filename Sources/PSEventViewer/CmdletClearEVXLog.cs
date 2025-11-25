using EventViewerX;
using System.Management.Automation;
using System.Threading.Tasks;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Clears entries from an event log using .NET APIs.</para>
/// <para type="description">Supports local or remote logs, optional retention policy update, and ShouldProcess confirmation.</para>
/// </summary>
/// <example>
///   <summary>Clear local Application log</summary>
///   <code>Clear-EVXLog -LogName Application</code>
///   <para>Clears the Application log on the current computer.</para>
/// </example>
/// <example>
///   <summary>Clear remote Security log with confirmation</summary>
///   <code>Clear-EVXLog -LogName Security -MachineName DC1 -Confirm</code>
///   <para>Prompts before clearing the Security log on DC1.</para>
/// </example>
/// <example>
///   <summary>Set retention days after clearing</summary>
///   <code>Clear-EVXLog -LogName "Microsoft-Windows-DHCP Server/Operational" -RetentionDays 30</code>
///   <para>Clears the log and sets a 30-day retention policy.</para>
/// </example>
[Cmdlet(VerbsCommon.Clear, "EVXLog", SupportsShouldProcess = true)]
[Alias("Clear-EventViewerXLog", "Clear-WinEventLog")]
[OutputType(typeof(bool))]
public sealed class CmdletClearEVXLog : AsyncPSCmdlet {
    /// <summary>Log name to clear.</summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string LogName { get; set; } = null!;

    /// <summary>Target computer.</summary>
    [Alias("ComputerName", "ServerName")]
    [Parameter]
    public string? MachineName { get; set; }

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

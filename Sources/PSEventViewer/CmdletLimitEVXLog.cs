using EventViewerX;
using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Threading.Tasks;

namespace PSEventViewer;

/// <summary>
/// Cmdlet wrapper around <see cref="SearchEvents.LimitLog"/> that exposes the
/// same options as the native <c>Limit-EventLog</c> cmdlet.
/// </summary>
/// <para>
/// Use <c>Limit-EVXLog</c> on PowerShell Core or when the built-in
/// <c>Limit-EventLog</c> cmdlet is unavailable. Both provide identical
/// functionality but this implementation relies solely on .NET APIs.
/// </para>
/// <para>
/// <paramref name="OverflowAction"/> determines how old events are handled:
/// <list type="bullet">
///   <item>
///     <term>OverwriteAsNeeded</term>
///     <description>Remove oldest events automatically when the log is full.</description>
///   </item>
///   <item>
///     <term>OverwriteOlder</term>
///     <description>Preserve events for <see cref="RetentionDays"/> days.</description>
///   </item>
///   <item>
///     <term>DoNotOverwrite</term>
///     <description>Stop logging when the log reaches the configured size.</description>
///   </item>
/// </list>
/// </para>
/// <seealso href="https://learn.microsoft.com/powershell/module/microsoft.powershell.management/limit-eventlog"/>
/// <example>
///   <summary>Use on PowerShell Core to retain events for seven days.</summary>
///   <code language="powershell">
///   Limit-EVXLog -LogName Application -MaximumKilobytes 20480 -OverflowAction OverwriteOlder -RetentionDays 7
///   </code>
///   <para>Equivalent to using <c>Limit-EventLog</c> on Windows PowerShell.</para>
/// </example>
/// <example>
///   <summary>Native cmdlet example for comparison.</summary>
///   <code language="powershell">
///   Limit-EventLog -LogName Application -MaximumSize 20480 -OverflowAction OverwriteOlder -RetentionDays 7
///   </code>
///   <para>Choose this when running Windows PowerShell with the management module available.</para>
/// </example>
[Cmdlet(VerbsCommon.Set, "EVXLogLimit", SupportsShouldProcess = true)]
[Alias("Limit-EventViewerXLog", "Limit-WinEventLog", "Limit-EVXLog")]
[OutputType(typeof(bool))]
public sealed class CmdletLimitEVXLog : AsyncPSCmdlet {
    /// <summary>Log name to modify.</summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string LogName { get; set; }

    /// <summary>Target machine.</summary>
    [Parameter]
    [Alias("ComputerName", "ServerName")]
    public string MachineName { get; set; }

    /// <summary>Maximum size in KB.</summary>
    [Parameter]
    public int MaximumKilobytes { get; set; }

    /// <summary>Overflow behavior.</summary>
    [Parameter]
    public OverflowAction OverflowAction { get; set; } = OverflowAction.OverwriteAsNeeded;

    /// <summary>Retention days for OverwriteOlder policy.</summary>
    [Parameter]
    public int RetentionDays { get; set; } = 7;

    /// <summary>Executes the log limiting.</summary>
    protected override Task ProcessRecordAsync() {
        var errorAction = GetErrorActionPreference();
        try {
            if (ShouldProcess($"{LogName} on {MachineName ?? "localhost"}", "Limit event log")) {
                bool result = SearchEvents.LimitLog(LogName, MachineName, MaximumKilobytes, OverflowAction, RetentionDays);
                WriteObject(result);
            } else {
                WriteObject(false);
            }
        } catch (Exception ex) {
            WriteWarning($"Limit-EVXLog - Error limiting log {LogName}: {ex.Message}");
            if (errorAction == ActionPreference.Stop) {
                ThrowTerminatingError(new ErrorRecord(ex, "LimitEVXLogFailed", ErrorCategory.InvalidOperation, LogName));
            } else {
                WriteObject(false);
            }
        }
        return Task.CompletedTask;
    }
}
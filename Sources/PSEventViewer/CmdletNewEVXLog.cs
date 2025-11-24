using System.Diagnostics;
using System.Management.Automation;
using System.Threading.Tasks;
using EventViewerX;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Creates a new Windows event log with optional size and retention settings.</para>
/// <para type="description">Wraps SearchEvents.CreateLog to add a log/provider pair locally or remotely, with overflow behavior and retention tuning.</para>
/// </summary>
/// <example>
///   <summary>Create custom log</summary>
///   <code>New-EVXLog -LogName MyApp -ProviderName MyApp</code>
///   <para>Creates a new log and provider for application events.</para>
/// </example>
/// <example>
///   <summary>Set size and overwrite policy</summary>
///   <code>New-EVXLog -LogName MyApp -MaximumKilobytes 102400 -OverflowAction OverwriteOlder -RetentionDays 30</code>
///   <para>Limits the log to ~100 MB and retains events for 30 days.</para>
/// </example>
/// <example>
///   <summary>Create log on remote server</summary>
///   <code>New-EVXLog -LogName MyApp -ProviderName MyApp -MachineName SRV01</code>
///   <para>Creates the log on SRV01.</para>
/// </example>
[Cmdlet(VerbsCommon.New, "EVXLog", SupportsShouldProcess = true)]
[Alias("New-EventViewerXLog", "New-WinEventLog")]
[OutputType(typeof(bool))]
public sealed class CmdletNewEVXLog : AsyncPSCmdlet {
    /// <summary>
    /// Name of the log to create.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string LogName { get; set; }

    /// <summary>
    /// Name of the provider associated with the log.
    /// </summary>
    [Alias("Source", "Provider")]
    [Parameter(Position = 1)]
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Optional log name to scope source creation checks (defaults to the log being created when provided).
    /// </summary>
    [Parameter]
    public string SourceLogName { get; set; }

    /// <summary>
    /// Target machine on which to create the log.
    /// </summary>
    [Alias("ComputerName", "ServerName")]
    [Parameter]
    public string MachineName { get; set; }

    /// <summary>
    /// Maximum log size in kilobytes.
    /// </summary>
    [Parameter]
    public int MaximumKilobytes { get; set; }

    /// <summary>
    /// Overflow behavior when the log is full.
    /// </summary>
    [Parameter]
    public OverflowAction OverflowAction { get; set; } = OverflowAction.OverwriteAsNeeded;

    /// <summary>
    /// Minimum days to retain events when using OverwriteOlder policy.
    /// </summary>
    [Parameter]
    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// Creates the event log with the specified options.
    /// </summary>
    protected override Task ProcessRecordAsync() {
        var errorAction = GetErrorActionPreference();
        if (string.IsNullOrEmpty(ProviderName)) {
            ProviderName = LogName;
        }

        try {
            if (ShouldProcess($"{LogName} on {MachineName ?? "localhost"}", "Create event log")) {
                var sourceLog = string.IsNullOrEmpty(SourceLogName) ? LogName : SourceLogName;
                bool result = SearchEvents.CreateLog(LogName, ProviderName, MachineName, MaximumKilobytes, OverflowAction, RetentionDays, sourceLog);
                WriteObject(result);
            } else {
                WriteObject(false);
            }
        } catch (Exception ex) {
            WriteWarning($"New-EVXLog - Error creating log {LogName}: {ex.Message}");
            if (errorAction == ActionPreference.Stop) {
                ThrowTerminatingError(new ErrorRecord(ex, "NewEVXLogFailed", ErrorCategory.InvalidOperation, LogName));
            } else {
                WriteObject(false);
            }
        }

        return Task.CompletedTask;
    }
}

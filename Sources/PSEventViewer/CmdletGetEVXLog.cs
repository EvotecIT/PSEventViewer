using System.Management.Automation;
using System.Threading.Tasks;
using EventViewerX;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Retrieves event log details by name.</para>
/// <para type="description">Lists log metadata (size, record count, status) on local or remote machines; supports wildcards.</para>
/// </summary>
/// <example>
///   <summary>List security log</summary>
///   <code>Get-EVXLog -LogName Security</code>
///   <para>Shows details for the Security log on the local computer.</para>
/// </example>
/// <example>
///   <summary>Query remote logs</summary>
///   <code>Get-EVXLog -LogName Application,System -MachineName SRV01</code>
///   <para>Retrieves Application and System log info from SRV01.</para>
/// </example>
/// <example>
///   <summary>Use wildcards</summary>
///   <code>Get-EVXLog -LogName "Microsoft-Windows-*"</code>
///   <para>Lists all Microsoft-Windows prefixed logs.</para>
/// </example>
[Cmdlet(VerbsCommon.Get, "EVXLog")]
[Alias("Get-EventViewerXLog", "Get-WinEventLog")]
[OutputType(typeof(EventLogDetails))]
[OutputType(typeof(EventLogDetailsResult))]
public sealed class CmdletGetEVXLog : AsyncPSCmdlet {
    /// <summary>
    /// Name of the log to retrieve. Wildcards supported.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string[] LogName { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Target machine to query.
    /// </summary>
    [Parameter]
    [Alias("ComputerName", "ServerName")]
    public string? MachineName { get; set; }

    /// <summary>
    /// Returns typed diagnostic results for successful, inaccessible, missing, and partially readable logs.
    /// </summary>
    [Parameter]
    public SwitchParameter AsResult { get; set; }

    /// <summary>
    /// Session-open timeout in milliseconds.
    /// </summary>
    [Parameter]
    [ValidateRange(1, int.MaxValue)]
    public int TimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Reads the oldest and newest event timestamps using the same session. This adds two indexed reads per log.
    /// </summary>
    [Parameter]
    public SwitchParameter IncludeEventTimes { get; set; }

    /// <summary>
    /// Queries the log information.
    /// </summary>
    protected override Task ProcessRecordAsync() {
        foreach (EventLogDetailsResult result in SearchEvents.DisplayEventLogResults(LogName, MachineName, TimeoutMs, IncludeEventTimes)) {
            if (AsResult) {
                WriteObject(result);
            } else {
                if (result.HasDiagnosticFailure) {
                    WriteWarning(result.DiagnosticMessage);
                }
                if (result.Details != null) {
                    WriteObject(result.Details);
                }
            }
        }

        return Task.CompletedTask;
    }
}

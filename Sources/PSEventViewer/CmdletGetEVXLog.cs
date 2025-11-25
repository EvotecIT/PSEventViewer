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
    /// Queries the log information.
    /// </summary>
    protected override Task ProcessRecordAsync() {
        foreach (var item in SearchEvents.DisplayEventLogs(LogName, MachineName)) {
            WriteObject(item);
        }

        return Task.CompletedTask;
    }
}

using System.Management.Automation;
using System.Threading.Tasks;
using EventViewerX;

namespace PSEventViewer;

/// <summary>
/// Retrieves event log details by name.
/// </summary>
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

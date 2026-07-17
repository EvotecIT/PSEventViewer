using EventViewerX;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading.Tasks;

namespace PSEventViewer;

/// <summary>Retrieves PowerShell execution-context events from live operational logs or exported EVTX files.</summary>
[Cmdlet(VerbsCommon.Get, "EVXPowerShellScriptExecution")]
[Alias("Get-PowerShellScriptExecution")]
[OutputType(typeof(PowerShellScriptExecutionInfo), typeof(PowerShellScriptQueryExecutionInfo))]
public sealed class CmdletGetEVXPowerShellScriptExecution : PowerShellScriptQueryCmdletBase {
    /// <summary>Maximum execution records to return per computer. Zero returns every matching record.</summary>
    [Parameter]
    [ValidateRange(0, int.MaxValue)]
    public int MaxEvents { get; set; }

    /// <summary>Retrieves execution-context records and writes completion information when requested.</summary>
    protected override Task ProcessRecordAsync() {
        string?[] machines = MachineName ?? new string?[] { null };
        foreach (string? machine in machines) {
            CancelToken.ThrowIfCancellationRequested();
            var queryInfo = new PowerShellScriptQueryExecutionInfo();
            using IEnumerator<PowerShellScriptExecutionInfo> executions = SearchEvents.GetPowerShellScriptExecution(
                type: Type,
                machineName: machine,
                eventLogPath: EventLogPath,
                dateFrom: DateFrom,
                dateTo: DateTo,
                maxEvents: MaxEvents,
                maxEventsScanned: MaxEventsScanned,
                executionInfo: queryInfo,
                cancellationToken: CancelToken).GetEnumerator();
            try {
                while (TryMoveNextRemote(executions, machine, queryInfo)) {
                    WriteObject(executions.Current);
                }
            } finally {
                WriteQueryCompletion(queryInfo);
            }
        }

        return Task.CompletedTask;
    }
}

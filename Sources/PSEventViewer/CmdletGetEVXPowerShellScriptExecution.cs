using EventViewerX;
using System;
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
            try {
                foreach (PowerShellScriptExecutionInfo execution in SearchEvents.GetPowerShellScriptExecution(
                             type: Type,
                             machineName: machine,
                             eventLogPath: EventLogPath,
                             dateFrom: DateFrom,
                             dateTo: DateTo,
                             maxEvents: MaxEvents,
                             maxEventsScanned: MaxEventsScanned,
                             executionInfo: queryInfo,
                             cancellationToken: CancelToken)) {
                    WriteObject(execution);
                }
            } catch (Exception ex) when (EventLogRemoteQueryFailureClassifier.TryClassify(machine, ex, out EventLogRemoteQueryFailureKind failureKind)) {
                queryInfo.RecordFailure(failureKind, ex.Message);
            } finally {
                WriteQueryCompletion(queryInfo);
            }
        }

        return Task.CompletedTask;
    }
}

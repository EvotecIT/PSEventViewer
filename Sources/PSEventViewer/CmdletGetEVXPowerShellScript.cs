using EventViewerX;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading.Tasks;

namespace PSEventViewer;

/// <summary>Retrieves PowerShell scripts from event logs and optionally saves them.</summary>
[Cmdlet(VerbsCommon.Get, "EVXPowerShellScript")]
[OutputType(typeof(RestoredPowerShellScript), typeof(PowerShellScriptQueryExecutionInfo), typeof(string))]
public sealed class CmdletGetEVXPowerShellScript : PowerShellScriptQueryCmdletBase {
    /// <summary>Destination directory where retrieved scripts should be saved.</summary>
    [Parameter]
    public string? Path { get; set; }

    /// <summary>When set, converts scripts back to their original formatting.</summary>
    [Parameter]
    public SwitchParameter Format { get; set; }

    /// <summary>Filters scripts to those containing the specified text.</summary>
    [Parameter]
    public string[]? ContainsText { get; set; }

    /// <summary>Maximum reconstructed scripts to return per computer. Zero returns every matching script.</summary>
    [Alias("MaxEvents")]
    [Parameter]
    [ValidateRange(0, int.MaxValue)]
    public int MaxScripts { get; set; }

    /// <summary>Maximum incomplete script groups retained while scanning.</summary>
    [Parameter]
    [ValidateRange(1, int.MaxValue)]
    public int MaxPendingScripts { get; set; } = PowerShellEventEngine.DefaultPowerShellScriptPendingLimit;

    /// <summary>Maximum event snapshots retained across incomplete script groups.</summary>
    [Parameter]
    [ValidateRange(1, int.MaxValue)]
    public int MaxCachedEvents { get; set; } = PowerShellEventEngine.DefaultPowerShellScriptEventCacheLimit;

    /// <summary>Retrieves matching scripts and writes each result or saved path to the pipeline.</summary>
    protected override Task ProcessRecordAsync() {
        string?[] machines = MachineName ?? new string?[] { null };
        foreach (string? machine in machines) {
            CancelToken.ThrowIfCancellationRequested();
            var queryInfo = new PowerShellScriptQueryExecutionInfo();
            using IEnumerator<RestoredPowerShellScript> scripts = PowerShellEventEngine.GetPowerShellScripts(
                type: Type,
                machineName: machine,
                eventLogPath: EventLogPath,
                dateFrom: DateFrom,
                dateTo: DateTo,
                format: Format.IsPresent,
                containsText: ContainsText,
                maxScripts: MaxScripts,
                maxEventsScanned: MaxEventsScanned,
                maxPendingScripts: MaxPendingScripts,
                maxCachedEvents: MaxCachedEvents,
                cancellationToken: CancelToken,
                executionInfo: queryInfo).GetEnumerator();
            try {
                while (TryMoveNextRemote(scripts, machine, queryInfo)) {
                    RestoredPowerShellScript script = scripts.Current;
                    if (!string.IsNullOrEmpty(Path)) {
                        string path = script.Save(Path!);
                        WriteObject(path);
                    } else {
                        WriteObject(script);
                    }
                }
            } finally {
                WriteQueryCompletion(queryInfo);
            }
        }

        return Task.CompletedTask;
    }
}

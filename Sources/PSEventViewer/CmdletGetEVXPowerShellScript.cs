using EventViewerX;
using System;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;

namespace PSEventViewer {
    /// <summary>
    /// Retrieves PowerShell scripts from event logs and optionally saves them.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "EVXPowerShellScript")]
    [Alias("Restore-EVXPowerShellScript", "Get-PowerShellScriptExecution", "Restore-PowerShellScript")]
    [OutputType(typeof(RestoredPowerShellScript))]
    public sealed class CmdletGetEVXPowerShellScript : AsyncPSCmdlet {
        /// <summary>
        /// Specifies the PowerShell edition to filter events for.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public PowerShellEdition Type { get; set; }

        /// <summary>
    /// Computer names to query. When not provided, the local machine is used.
    /// </summary>
    [Alias("ComputerName")]
    [Parameter]
    public string?[]? MachineName { get; set; }

        /// <summary>
    /// Path to an exported event log file containing PowerShell script events.
    /// </summary>
    [Parameter]
    public string? EventLogPath { get; set; }

        /// <summary>
    /// Destination directory where retrieved scripts should be saved.
    /// </summary>
    [Parameter]
    public string? Path { get; set; }

        /// <summary>
        /// Only return scripts logged after this date.
        /// </summary>
        [Parameter]
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// Only return scripts logged before this date.
        /// </summary>
        [Parameter]
        public DateTime? DateTo { get; set; }

        /// <summary>
        /// When set, converts scripts back to their original formatting.
        /// </summary>
        [Parameter]
        public SwitchParameter Format { get; set; }

        /// <summary>
        /// Filters scripts to those containing the specified text.
        /// </summary>
        [Parameter]
        public string[]? ContainsText { get; set; }

        /// <summary>
        /// Maximum number of reconstructed scripts to return per computer. Zero returns every matching script.
        /// </summary>
        [Alias("MaxEvents")]
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public int MaxScripts { get; set; }

        /// <summary>
        /// Maximum number of native event records to scan per computer. Zero scans the complete query.
        /// </summary>
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public int MaxEventsScanned { get; set; }

        /// <summary>
        /// Maximum number of incomplete script groups retained while scanning.
        /// </summary>
        [Parameter]
        [ValidateRange(1, int.MaxValue)]
        public int MaxPendingScripts { get; set; } = SearchEvents.DefaultPowerShellScriptPendingLimit;

        /// <summary>
        /// Maximum number of event snapshots retained across incomplete script groups.
        /// </summary>
        [Parameter]
        [ValidateRange(1, int.MaxValue)]
        public int MaxCachedEvents { get; set; } = SearchEvents.DefaultPowerShellScriptEventCacheLimit;

        /// <summary>
        /// Retrieves matching scripts from event logs and writes them to the pipeline or disk.
        /// </summary>
        protected override Task ProcessRecordAsync() {
            var machines = MachineName ?? new string?[] { null };
            foreach (var machine in machines) {
                CancelToken.ThrowIfCancellationRequested();
                foreach (var script in SearchEvents.GetPowerShellScripts(
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
                             cancellationToken: CancelToken)) {
                    if (!string.IsNullOrEmpty(Path)) {
                        string path = script.Save(Path!);
                        WriteObject(path);
                    } else {
                        WriteObject(script);
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}

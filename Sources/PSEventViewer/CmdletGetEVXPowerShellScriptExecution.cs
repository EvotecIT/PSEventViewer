using EventViewerX;
using System;
using System.Management.Automation;
using System.Threading.Tasks;

namespace PSEventViewer {
    /// <summary>
    /// Retrieves PowerShell script execution details from event logs.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "EVXPowerShellScriptExecution")]
    [Alias("Get-PowerShellScriptExecution")]
    [OutputType(typeof(PowerShellScriptExecutionInfo))]
    public sealed class CmdletGetEVXPowerShellScriptExecution : AsyncPSCmdlet {
        /// <summary>
        /// Edition of PowerShell that generated the logs.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public PowerShellEdition Type { get; set; }

        /// <summary>
        /// Target computers to read events from.
        /// </summary>
        [Alias("ComputerName")]
        [Parameter]
        public string[] MachineName { get; set; }

        /// <summary>
        /// Path to an event log file.
        /// </summary>
        [Parameter]
        public string EventLogPath { get; set; }

        /// <summary>
        /// Start time filter.
        /// </summary>
        [Parameter]
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// End time filter.
        /// </summary>
        [Parameter]
        public DateTime? DateTo { get; set; }

        /// <summary>
        /// Processes the request and returns execution info.
        /// </summary>
        protected override Task ProcessRecordAsync() {
            var machines = MachineName ?? new string[] { null };
            foreach (var machine in machines) {
                foreach (var info in SearchEvents.GetPowerShellScriptExecution(Type, machine, EventLogPath, DateFrom, DateTo)) {
                    WriteObject(info);
                }
            }
            return Task.CompletedTask;
        }
    }
}

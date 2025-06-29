using EventViewerX;
using System;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;

namespace PSEventViewer {
    /// <summary>
    /// Reconstructs PowerShell scripts from event logs.
    /// </summary>
    [Cmdlet(VerbsData.Restore, "EVXPowerShellScript")]
    [Alias("Restore-PowerShellScript")]
    [OutputType(typeof(RestoredPowerShellScript))]
    public sealed class CmdletRestoreEVXPowerShellScript : AsyncPSCmdlet {
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
        /// Destination folder for restored scripts. If not specified, scripts are returned.
        /// </summary>
        [Parameter]
        public string Path { get; set; }

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
        /// Attempts to format scripts.
        /// </summary>
        [Parameter]
        public SwitchParameter Format { get; set; }

        /// <summary>
        /// Return only scripts containing these strings.
        /// </summary>
        [Parameter]
        public string[] ContainsText { get; set; }

        /// <summary>
        /// Processes the request and outputs restored scripts or file paths.
        /// </summary>
        protected override Task ProcessRecordAsync() {
            var machines = MachineName ?? new string[] { null };
            foreach (var machine in machines) {
                foreach (var script in SearchEvents.RestorePowerShellScripts(Type, machine, EventLogPath, DateFrom, DateTo, Format.IsPresent, ContainsText)) {
                    if (!string.IsNullOrEmpty(Path)) {
                        Directory.CreateDirectory(Path);
                        string fileName = $"{script.EventRecord.MachineName}_{script.ScriptBlockId}.ps1";
                        string filePath = System.IO.Path.Combine(Path, fileName);
                        string header = string.Join(Environment.NewLine,
                            "<#",
                            $"RecordID = {script.EventRecord.RecordId}",
                            $"LogName = {script.EventRecord.LogName}",
                            $"MachineName = {script.EventRecord.MachineName}",
                            $"TimeCreated = {script.EventRecord.TimeCreated}",
                            "#>");
                        File.WriteAllText(filePath, header + Environment.NewLine + script.Script);
                        string zonePath = filePath + ":Zone.Identifier";
                        File.WriteAllText(zonePath, "[ZoneTransfer]\r\nZoneId=3");
                        WriteObject(filePath);
                    } else {
                        WriteObject(script);
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}

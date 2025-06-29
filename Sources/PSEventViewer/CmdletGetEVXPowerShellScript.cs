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
        [Parameter(Mandatory = true, Position = 0)]
        public PowerShellEdition Type { get; set; }

        [Alias("ComputerName")]
        [Parameter]
        public string[] MachineName { get; set; }

        [Parameter]
        public string EventLogPath { get; set; }

        [Parameter]
        public string Path { get; set; }

        [Parameter]
        public DateTime? DateFrom { get; set; }

        [Parameter]
        public DateTime? DateTo { get; set; }

        [Parameter]
        public SwitchParameter Format { get; set; }

        [Parameter]
        public string[] ContainsText { get; set; }

        protected override Task ProcessRecordAsync() {
            var machines = MachineName ?? new string[] { null };
            foreach (var machine in machines) {
                foreach (var script in SearchEvents.GetPowerShellScripts(Type, machine, EventLogPath, DateFrom, DateTo, Format.IsPresent, ContainsText)) {
                    if (!string.IsNullOrEmpty(Path)) {
                        string path = script.Save(Path);
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

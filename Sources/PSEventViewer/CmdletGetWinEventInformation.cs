using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using EventViewerX;

namespace PSEventViewer {
    [Cmdlet(VerbsCommon.Get, "WinEventInformation")]
    [OutputType(typeof(WinEventInformation))]
    public sealed class CmdletGetWinEventInformation : AsyncPSCmdlet {
        [Alias("ADDomainControllers", "DomainController", "Server", "Servers", "Computer", "Computers", "ComputerName")]
        [Parameter(Mandatory = false)]
        public List<string> Machine { get; set; } = new() { Environment.MachineName };

        [Parameter(Mandatory = false)]
        public List<string> FilePath { get; set; }

        [Alias("LogType", "Log")]
        [Parameter(Mandatory = false)]
        public List<string> LogName { get; set; } = new() { "Security" };

        [Parameter(Mandatory = false)]
        public int MaxRunspaces { get; set; } = 50;

        [Alias("AskDC", "QueryDomainControllers", "AskForest")]
        [Parameter(Mandatory = false)]
        public SwitchParameter RunAgainstDC { get; set; }

        private SearchEvents SearchEvents;
        private InternalLoggerPowerShell Logger;

        protected override Task BeginProcessingAsync() {
            var internalLogger = new InternalLogger(false);
            Logger = new InternalLoggerPowerShell(internalLogger, WriteVerbose, WriteWarning, WriteDebug, WriteError, WriteProgress, WriteInformation);
            SearchEvents = new SearchEvents(internalLogger);
            return Task.CompletedTask;
        }

        protected override Task ProcessRecordAsync() {
            var machines = Machine ?? new List<string>();
            if (RunAgainstDC.IsPresent) {
                try {
                    var forest = System.DirectoryServices.ActiveDirectory.Forest.GetCurrentForest();
                    machines = forest.Domains.Cast<System.DirectoryServices.ActiveDirectory.Domain>()
                        .SelectMany(d => d.DomainControllers.Cast<System.DirectoryServices.ActiveDirectory.DomainController>())
                        .Select(dc => dc.Name).Distinct().ToList();
                } catch {
                    // ignored
                }
            }

            foreach (var info in SearchEvents.GetWinEventInformation(LogName?.ToArray(), machines, FilePath, MaxRunspaces)) {
                WriteObject(info);
            }
            return Task.CompletedTask;
        }
    }
}

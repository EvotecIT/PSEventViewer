using EventViewerX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

namespace PSEventViewer {
    /// <summary>
    /// Comprehensive cmdlet for retrieving Windows Event Log information and settings.
    /// Supports single or multiple logs, machines, and files with parallel processing.
    /// Consolidates functionality from Get-EventsSettings and Get-WinEventSettings.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "EVXInfo")]
    [Alias("Get-EventViewerXInfo", "Get-EventsSettings", "Get-EventsInformation")]
    [OutputType(typeof(WinEventInformation))]
    public sealed class CmdletGetEVXInfo : AsyncPSCmdlet {
        /// <summary>
        /// Target machines from which to gather log information.
        /// </summary>
        [Alias("ADDomainControllers", "DomainController", "Server", "Servers", "Computer", "Computers", "ComputerName")]
        [Parameter(Mandatory = false)]
        public List<string> Machine { get; set; } = new() { Environment.MachineName };

        /// <summary>
        /// Paths to event log files to analyse.
        /// </summary>
        [Parameter(Mandatory = false)]
        public List<string> FilePath { get; set; }

        /// <summary>
        /// Names of logs to retrieve information about.
        /// </summary>
        [Alias("LogType", "Log")]
        [Parameter(Mandatory = false)]
        public List<string> LogName { get; set; } = new() { "Security" };

        /// <summary>
        /// Maximum number of runspaces used when querying multiple machines.
        /// </summary>
        [Parameter(Mandatory = false)]
        public int MaxRunspaces { get; set; } = 50;

        /// <summary>
        /// When set, queries domain controllers instead of specified machines.
        /// </summary>
        [Alias("AskDC", "QueryDomainControllers", "AskForest")]
        [Parameter(Mandatory = false)]
        public SwitchParameter RunAgainstDC { get; set; }

        /// <summary>
        /// Retrieves event log information based on the provided parameters.
        /// </summary>
        protected override Task ProcessRecordAsync() {
            // Create fresh instances for each invocation to prevent state retention
            var internalLogger = new InternalLogger(false);
            var logger = new InternalLoggerPowerShell(internalLogger, WriteVerbose, WriteWarning, WriteDebug, WriteError, WriteProgress, WriteInformation);
            var searchEvents = new SearchEvents(internalLogger);

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

            // Determine what was explicitly specified by the user
            bool machineSpecified = MyInvocation.BoundParameters.ContainsKey("Machine") || RunAgainstDC.IsPresent;
            bool logNameSpecified = MyInvocation.BoundParameters.ContainsKey("LogName");
            bool filePathSpecified = MyInvocation.BoundParameters.ContainsKey("FilePath");

            // If only FilePath is specified, don't process machine/log defaults
            if (filePathSpecified && !machineSpecified && !logNameSpecified) {
                // Only process files
                foreach (var info in SearchEvents.GetWinEventInformation(null, null, FilePath, MaxRunspaces)) {
                    WriteObject(info);
                }
            } else {
                // Process logs/machines (use defaults if nothing specified)
                List<string> logsToProcess = logNameSpecified ? LogName : new List<string> { "Security" };
                List<string> machinesToProcess = machineSpecified ? machines : new List<string> { Environment.MachineName };

                foreach (var info in SearchEvents.GetWinEventInformation(logsToProcess?.ToArray(), machinesToProcess, FilePath, MaxRunspaces)) {
                    WriteObject(info);
                }
            }

            return Task.CompletedTask;
        }
    }
}
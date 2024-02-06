using System.Collections.Generic;
using System;
using System.Management.Automation;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace PSEventViewer.PowerShell {

    [Cmdlet(VerbsCommon.Get, "PSEvent")]
    public sealed class EventSearch : PSCmdlet {
        [Parameter(Mandatory = true, Position = 0)] public string LogName;
        [Parameter(Mandatory = false)] public List<int> EventId = null;
        [Alias("ComputerName", "ServerName")]
        [Parameter(Mandatory = false)] public List<string> MachineName = null;
        [Parameter(Mandatory = false)] public string ProviderName = null;
        [Parameter(Mandatory = false)] public Keywords? Keywords = null;
        [Parameter(Mandatory = false)] public Level? Level = null;
        [Parameter(Mandatory = false)] public DateTime? StartTime = null;
        [Parameter(Mandatory = false)] public DateTime? EndTime = null;
        [Parameter(Mandatory = false)] public string UserId = null;
        [Parameter(Mandatory = false)] public int NumberOfThreads { get; set; } = 8;
        [Parameter(Mandatory = false)] public int MaxEvents = 0;
        [Parameter(Mandatory = false)] public Modes Mode { get; set; } = Modes.Parallel;

        protected override void BeginProcessing() {
            // Initialize the logger
            var internalLogger = new InternalLogger(false);
            // Initialize the PowerShell logger, and subscribe to the verbose message event
            var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
            var eventSearching = new EventSearching(internalLogger);
        }
        protected override void ProcessRecord() {
            if (Mode == Modes.Disabled) {
                foreach (var machine in MachineName) {
                    foreach (var eventObject in EventSearching.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents)) {
                        WriteObject(eventObject);
                    }
                }
            } else if (Mode == Modes.Parallel) {
                foreach (var eventObject in EventSearching.QueryLogsParallel(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, NumberOfThreads)) {
                    WriteObject(eventObject);
                }
            } else if (Mode == Modes.ParallelForEach) {
                var options = new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads };
                Parallel.ForEach(MachineName, options, machine => {
                    foreach (var eventObject in EventSearching.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents)) {
                        WriteObject(eventObject);
                    }
                });
            } else if (Mode == Modes.ParallelForEachBuiltin) {
                foreach (var eventObject in EventSearching.QueryLogsParallelForEach(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, NumberOfThreads)) {
                    WriteObject(eventObject);
                }
            }
        }
        protected override void EndProcessing() {
            base.EndProcessing();
        }
    }
}
using System.Collections.Generic;
using System;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PSEventViewer.PowerShell {

    [Cmdlet(VerbsCommon.Find, "WinEvent", DefaultParameterSetName = "GenericEvents")]
    public sealed class CmdletFindEvent : AsyncPSCmdlet {
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "GenericEvents")] public string LogName;
        [Parameter(Mandatory = false, Position = 1, ParameterSetName = "GenericEvents")] public List<int> EventId = null;

        [Alias("ComputerName", "ServerName")]
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        public List<string> MachineName = null;

        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")] public string ProviderName = null;
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")] public Keywords? Keywords = null;
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")] public Level? Level = null;
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")] public DateTime? StartTime = null;
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")] public DateTime? EndTime = null;
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")] public string UserId = null;

        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        public int NumberOfThreads { get; set; } = 8;

        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        public int MaxEvents = 0;

        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        public Modes Mode { get; set; } = Modes.Parallel;


        [Parameter(Mandatory = true, ParameterSetName = "NamedEvents")] public List<NamedEvents> Type;

        protected override Task BeginProcessingAsync() {
            // Initialize the logger to be able to see verbose, warning, debug, error, progress, and information messages.
            var internalLogger = new InternalLogger(false);
            var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
            var eventSearching = new EventSearching(internalLogger);
            return Task.CompletedTask;
        }
        protected override Task ProcessRecordAsync() {

            if (Type != null) {
                // let's find the events prepared for search
                foreach (var eventObject in EventSearchingTargeted.FindEventsByNamedEventsOld(MachineName, Type)) {
                    WriteObject(eventObject);
                }
            } else {
                // Lets find the events by generic log name, event id, machine name, provider name, keywords, level, start time, end time, user id, and max events.
                if (Mode == Modes.Disabled) {
                    foreach (var machine in MachineName) {
                        foreach (var eventObject in EventSearching.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents)) {
                            WriteObject(eventObject);
                        }
                    }
                } else if (Mode == Modes.Parallel) {
                    foreach (var eventObject in EventSearching.QueryLogsParallel(LogName, EventId, MachineName, maxThreads: NumberOfThreads)) {
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

            return Task.CompletedTask;
        }
    }
}
using System.Collections.Generic;
using System;
using System.Management.Automation;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics.Eventing.Reader;

namespace PSEventViewer.PowerShell {

    //[OutputType(typeof(EventRecord), ParameterSetName = new string[] { "GetLogSet", "GetProviderSet", "FileSet", "HashQuerySet", "XmlQuerySet" })]
    //[OutputType(typeof(ProviderMetadata), ParameterSetName = new string[] { "ListProviderSet" })]
    //[OutputType(typeof(EventLogConfiguration), ParameterSetName = new string[] { "ListLogSet" })]
    [Cmdlet(VerbsCommon.Find, "WinEvent", DefaultParameterSetName = "GenericEvents")]
    public sealed class CmdletFindEvent : AsyncPSCmdlet {
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "RecordId")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "GenericEvents")]
        public string LogName;

        [Parameter(Mandatory = false, Position = 1, ParameterSetName = "GenericEvents")]
        public List<int> EventId = null;

        [Alias("RecordId")]
        [Parameter(Mandatory = false, Position = 1, ParameterSetName = "RecordId")]
        public List<long> EventRecordId = null;

        [Alias("ComputerName", "ServerName")]
        [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "ListLog")]
        public List<string> MachineName;

        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        public string ProviderName;

        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        public Keywords? Keywords;

        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        public Level? Level;

        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        public DateTime? StartTime;

        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        public DateTime? EndTime;

        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        public TimePeriod? TimePeriod;

        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        public string UserId;

        [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        public int NumberOfThreads = 8;

        [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        public int MaxEvents = 0;

        [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        public ParallelOption ParallelOption = ParallelOption.Parallel;


        [Parameter(Mandatory = true, ParameterSetName = "NamedEvents")]
        public NamedEvents[] Type;


        [Parameter(Mandatory = true, ParameterSetName = "ListLog")]
        public string[] ListLog { get; set; } = { "*" };


        protected override Task BeginProcessingAsync() {
            // Initialize the logger to be able to see verbose, warning, debug, error, progress, and information messages.
            var internalLogger = new InternalLogger(false);
            var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
            var searchEvents = new SearchEvents(internalLogger);
            return Task.CompletedTask;
        }
        protected override Task ProcessRecordAsync() {
            if (ParameterSetName == "ListLog") {
                foreach (var log in SearchEvents.DisplayEventLogsParallel(ListLog, MachineName, NumberOfThreads)) {
                    WriteObject(log);
                }
            } else {
                if (Type != null) {
                    // let's find the events prepared for search
                    List<NamedEvents> typeList = Type.ToList();
                    foreach (var eventObject in SearchEvents.FindEventsByNamedEvents(typeList, MachineName, StartTime, EndTime, TimePeriod, maxThreads: NumberOfThreads, maxEvents: MaxEvents)) {
                        WriteObject(eventObject);
                    }
                } else {
                    // Let's find the events by generic log name, event id, machine name, provider name, keywords, level, start time, end time, user id, and max events.
                    if (ParallelOption == ParallelOption.Disabled) {
                        if (MachineName == null) {
                            foreach (var eventObject in SearchEvents.QueryLog(LogName, EventId, null, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod)) {
                                WriteObject(eventObject);
                            }
                        } else {
                            foreach (var machine in MachineName) {
                                foreach (var eventObject in SearchEvents.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod)) {
                                    WriteObject(eventObject);
                                }
                            }
                        }
                    } else if (ParallelOption == ParallelOption.Parallel) {
                        foreach (var eventObject in SearchEvents.QueryLogsParallel(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, NumberOfThreads, EventRecordId, TimePeriod)) {
                            WriteObject(eventObject);
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
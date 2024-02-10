using System.Collections.Generic;
using System;
using System.Management.Automation;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;

namespace PSEventViewer.PowerShell {

    [Cmdlet(VerbsCommon.Find, "GenericEvent")]
    public sealed class EventSearch : AsyncPSCmdlet {
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

        private EventSearching eventSearching;

        protected override void BeginProcessing() {
            WriteVerbose("Test0");
            // Initialize the logger
            var internalLogger = new InternalLogger(false);
            //Initialize the PowerShell logger, and subscribe to the verbose message event
            //var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, null, this.WriteProgress, this.WriteInformation);
            var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
            eventSearching = new EventSearching(internalLogger);
            eventSearching.Verbose = false;
            eventSearching.Error = true;
            eventSearching.Warning = true;

            WriteVerbose("Test1");
        }
        protected override void ProcessRecord() {


            //if (Mode == Modes.Disabled) {
            //    foreach (var machine in MachineName) {
            //        foreach (var eventObject in EventSearching.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents)) {
            //            WriteObject(eventObject);
            //        }
            //    }
            //} else if (Mode == Modes.Parallel) {
            WriteVerbose("Test2");
            //WriteObject(EventSearching.QueryLogsParallel(LogName, EventId, MachineName, maxThreads: 0), true);
            //} else if (Mode == Modes.ParallelForEach) {
            //    var options = new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads };
            //    Parallel.ForEach(MachineName, options, machine => {
            //        foreach (var eventObject in EventSearching.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents)) {
            //            WriteObject(eventObject);
            //        }
            //    });
            //} else if (Mode == Modes.ParallelForEachBuiltin) {
            //    foreach (var eventObject in EventSearching.QueryLogsParallelForEach(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, NumberOfThreads)) {
            //        WriteObject(eventObject);
            //    }
            //} else if (Mode == Modes.Async) {

            //}
        }
        protected override void EndProcessing() {
            WriteVerbose("Test3");

            base.EndProcessing();
        }
    }
}
using System.Collections.Generic;
using System;
using System.Management.Automation;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;

namespace PSEventViewer.PowerShell {

    [Cmdlet(VerbsCommon.Find, "GenericTest")]
    public sealed class EventSearchTest : AsyncPSCmdlet {
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

        protected override Task BeginProcessingAsync() {
            // Initialize the logger to be able to see verbose, warning, debug, error, progress, and information messages.
            var internalLogger = new InternalLogger(false);
            var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
            var eventSearching = new EventSearching(internalLogger);
            return Task.CompletedTask;
        }
        protected override Task ProcessRecordAsync() {
            //WriteObject(EventSearching.QueryLogsParallel(LogName, EventId, MachineName, maxThreads: NumberOfThreads), true);
            foreach (var eventObject in EventSearching.QueryLogsParallel(LogName, EventId, MachineName, maxThreads: NumberOfThreads)) {
                WriteObject(eventObject);
            }
            return Task.CompletedTask;
        }
    }
}
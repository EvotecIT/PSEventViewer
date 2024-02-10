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

        private EventSearching eventSearching;
        private InternalLogger _logger;

        protected override Task BeginProcessingAsync() {
            // Initialize the logger
            var internalLogger = new InternalLogger(false);
            //Initialize the PowerShell logger, and subscribe to the verbose message event
            //var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, null, this.WriteProgress, this.WriteInformation);
            var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
            eventSearching = new EventSearching(internalLogger);
            eventSearching.Verbose = false;
            eventSearching.Error = true;
            eventSearching.Warning = true;
            _logger = internalLogger;
            return Task.CompletedTask;
        }
        protected override Task ProcessRecordAsync() {
            WriteVerbose("foo");
            //WriteObject(EventSearching.QueryLogsParallel(LogName, EventId, MachineName, maxThreads: 0), true);
            foreach (var eventObject in EventSearching.QueryLogsParallel(LogName, EventId, MachineName, maxThreads: 0)) {
                WriteObject(eventObject);
            }

            _logger.WriteVerbose("fo1o");

            // WriteObject("testMe");

            WriteVerbose("foo2");
            return Task.CompletedTask;
        }
    }
}
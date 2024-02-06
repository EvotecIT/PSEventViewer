using System.Collections.Generic;
using System;
using System.Management.Automation;
using System.Xml.Linq;

namespace PSEventViewer.PowerShell {

    [Cmdlet(VerbsCommon.Get, "PSEvent")]
    public sealed class EventSearch : PSCmdlet {
        [Parameter(Mandatory = true, Position = 0)] public string LogName;
        [Parameter(Mandatory = false)] public List<int> EventId = null;
        [Alias("ComputerName", "ServerName")]
        [Parameter(Mandatory = false)] public string MachineName = null;
        [Parameter(Mandatory = false)] public string ProviderName = null;
        [Parameter(Mandatory = false)] public Keywords? Keywords = null;
        [Parameter(Mandatory = false)] public Level? Level = null;
        [Parameter(Mandatory = false)] public DateTime? StartTime = null;
        [Parameter(Mandatory = false)] public DateTime? EndTime = null;
        [Parameter(Mandatory = false)] public string UserId = null;
        [Parameter(Mandatory = false)] public int NumberOfThreads { get; set; } = 8;
        [Parameter(Mandatory = false)] public int MaxEvents = 0;

        protected override void BeginProcessing() {
            // Initialize the logger
            var internalLogger = new InternalLogger(false);
            // Initialize the PowerShell logger, and subscribe to the verbose message event
            var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
            var eventSearching = new EventSearching(internalLogger);
        }
        protected override void ProcessRecord() {
            foreach (var eventObject in EventSearching.QueryLog(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents)) {
                WriteObject(eventObject);
            }
        }
        protected override void EndProcessing() {
            base.EndProcessing();
        }
    }
}
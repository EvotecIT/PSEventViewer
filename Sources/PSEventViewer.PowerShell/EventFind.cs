using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PSEventViewer.PowerShell {
    [Cmdlet(VerbsCommon.Find, "NamedEvent")]
    public sealed class EventFind : AsyncPSCmdlet {
        [Alias("ComputerName", "ServerName")]
        [Parameter(Mandatory = false)] public List<string> MachineName = null;
        [Parameter(Mandatory = false)] public DateTime? StartTime = null;
        [Parameter(Mandatory = false)] public DateTime? EndTime = null;
        [Parameter(Mandatory = false)] public int NumberOfThreads { get; set; } = 8;
        [Parameter(Mandatory = false)] public int MaxEvents = 0;
        [Parameter(Mandatory = false)] public Modes Mode { get; set; } = Modes.Parallel;

        [Parameter(Mandatory = true)] public List<NamedEvents> Type;

        private EventSearchingTargeted eventSearching;
        private EventSearching eventSearching1;

        protected override void BeginProcessing() {
            // Initialize the logger
            var internalLogger = new InternalLogger(false);
            // Initialize the PowerShell logger, and subscribe to the verbose message event
            var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
            eventSearching = new EventSearchingTargeted(internalLogger);
            eventSearching.Error = true;
            eventSearching.Warning = true;
        }
        protected override void ProcessRecord() {
            if (MachineName == null) {
                MachineName = new List<string> { Environment.MachineName };
            }
            var foundEvents = EventSearchingTargeted.FindEventsByNamedEvents(MachineName, Type).GetAwaiter().GetResult();
            foreach (var foundEvent in foundEvents) {
                WriteObject(foundEvent);
            }
        }
        protected override void EndProcessing() {
            base.EndProcessing();
        }
    }
}

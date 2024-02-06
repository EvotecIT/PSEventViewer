using System.Management.Automation;

namespace PSEventViewer.PowerShell {

    [Cmdlet(VerbsLifecycle.Start, "EventWatching")]
    public sealed class EventWatchingStart : PSCmdlet {
        private EventWatching EventWatching { get; set; }

        [Parameter(Mandatory = false, Position = 1)] public int NumberOfThreads { get; set; } = 8;

        protected override void BeginProcessing() {
            // Initialize the logger
            var internalLogger = new InternalLogger(false);
            // Initialize the PowerShell logger, and subscribe to the verbose message event
            var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
            // Initialize the LingeringObjects class
            EventWatching = new EventWatching(internalLogger) {
                NumberOfThreads = NumberOfThreads
            };
        }

        protected override void ProcessRecord() {


        }
        protected override void EndProcessing() {
            //this.WriteObject(lingeringObjects);
            base.EndProcessing();
        }
    }
}
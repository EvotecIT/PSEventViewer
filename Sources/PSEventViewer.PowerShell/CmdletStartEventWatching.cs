using System.Management.Automation;
using System.Threading.Tasks;

namespace PSEventViewer.PowerShell {
    [Cmdlet(VerbsLifecycle.Start, "EventWatching")]
    public sealed class CmdletStartEventWatching : AsyncPSCmdlet {
        private WatchEvents EventWatching { get; set; }

        [Parameter(Mandatory = false, Position = 1)] public int NumberOfThreads { get; set; } = 8;

        protected override Task BeginProcessingAsync() {
            // Initialize the logger to be able to see verbose, warning, debug, error, progress, and information messages.
            var internalLogger = new InternalLogger(false);
            var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);

            EventWatching = new WatchEvents(internalLogger) {
                NumberOfThreads = NumberOfThreads
            };
            return Task.CompletedTask;
        }
        protected override Task ProcessRecordAsync() {

            return Task.CompletedTask;
        }
        protected override Task EndProcessingAsync() {

            return Task.CompletedTask;
        }
    }
}
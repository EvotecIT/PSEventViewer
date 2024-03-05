using System.Management.Automation;
using System.Threading.Tasks;
using EventViewer;

namespace PSEventViewer.PowerShell {
    [Cmdlet(VerbsCommunications.Write, "WinEvent")]
    public sealed class CmdletWriteWinEvent : AsyncPSCmdlet {
        protected override Task BeginProcessingAsync() {
            // Initialize the logger to be able to see verbose, warning, debug, error, progress, and information messages.
            var internalLogger = new InternalLogger(false);
            var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
            var searchEvents = new SearchEvents(internalLogger);
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
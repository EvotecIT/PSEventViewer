using System.Management.Automation;
using System.Threading.Tasks;
using EventViewerX;

namespace PSEventViewer {
    [Cmdlet(VerbsCommon.Get, "WinEventSettings")]
    [OutputType(typeof(EventLogDetails))]
    public sealed class CmdletGetWinEventSettings : AsyncPSCmdlet {
        [Parameter(Mandatory = true, Position = 0)]
        public string LogName { get; set; }

        [Alias("ComputerName", "ServerName")]
        [Parameter(Mandatory = false)]
        public string ComputerName { get; set; }

        protected override Task BeginProcessingAsync() {
            var internalLogger = new InternalLogger(false);
            var internalLoggerPowerShell = new InternalLoggerPowerShell(
                internalLogger,
                this.WriteVerbose,
                this.WriteWarning,
                this.WriteDebug,
                this.WriteError,
                this.WriteProgress,
                this.WriteInformation);
            return Task.CompletedTask;
        }

        protected override Task ProcessRecordAsync() {
            foreach (var log in SearchEvents.DisplayEventLogs(new[] { LogName }, ComputerName)) {
                WriteObject(log);
            }
            return Task.CompletedTask;
        }
    }
}

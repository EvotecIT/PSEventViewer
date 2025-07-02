using EventViewerX;
using System.Management.Automation;
using System.Threading.Tasks;
using System.Linq;

namespace PSEventViewer {
    /// <summary>
    /// Starts real-time monitoring of Windows Event Logs with customizable filters and actions.
    /// Provides continuous event watching with scriptblock execution for matched events.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "EVXWatcher")]
    [Alias("Start-EventViewerXWatcher", "Start-EventWatching")]
    [OutputType(typeof(void))]
    public sealed class CmdletStartEVXWatcher : AsyncPSCmdlet {
        private WatchEvents EventWatching { get; set; }

        /// <summary>
        /// Name of the computer to monitor events on.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string MachineName { get; set; }

        /// <summary>
        /// Name of the log to watch on the specified machine.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        public string LogName { get; set; }

        /// <summary>
        /// Array of event identifiers to monitor.
        /// </summary>
        [Parameter(Mandatory = true, Position = 2)]
        public int[] EventId { get; set; }

        /// <summary>
        /// Script block executed when matching events are detected.
        /// </summary>
        [Parameter(Mandatory = true, Position = 3)]
        public ScriptBlock Action { get; set; }

        /// <summary>
        /// Number of threads used for event processing.
        /// </summary>
        [Parameter(Mandatory = false)]
        [ValidateRange(1, int.MaxValue)]
        public int NumberOfThreads { get; set; } = 8;

        /// <summary>
        /// Initializes resources prior to processing.
        /// </summary>
        protected override Task BeginProcessingAsync() {
            // Initialize the logger to be able to see verbose, warning, debug, error, progress, and information messages.
            var internalLogger = new InternalLogger(false);
            var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);

            EventWatching = new WatchEvents(internalLogger) {
                NumberOfThreads = NumberOfThreads
            };
            return Task.CompletedTask;
        }
        /// <summary>
        /// Starts watching for events and invokes the provided action.
        /// </summary>
        protected override Task ProcessRecordAsync() {
            EventWatching.Watch(MachineName, LogName, EventId.ToList(), e => Action.Invoke(e), CancelToken);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Performs cleanup after event processing completes.
        /// </summary>
        protected override Task EndProcessingAsync() {

            return Task.CompletedTask;
        }
    }
}
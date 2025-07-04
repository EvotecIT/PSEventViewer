using EventViewerX;
using System;
using System.Management.Automation;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace PSEventViewer {
    /// <summary>
    /// Starts real-time monitoring of Windows Event Logs with customizable filters and actions.
    /// Provides continuous event watching with scriptblock execution for matched events.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "EVXWatcher", DefaultParameterSetName = "Direct")]
    [Alias("Start-EventViewerXWatcher", "Start-EventWatching")]
    [OutputType(typeof(string))]
    public sealed class CmdletStartEVXWatcher : AsyncPSCmdlet {
        private InternalLogger _internalLogger;

        private class WatcherDefinition {
            public string Name { get; set; }
            public string MachineName { get; set; }
            public string LogName { get; set; }
            public int[] EventId { get; set; }
            public ScriptBlock Action { get; set; }
            public int NumberOfThreads { get; set; }
        }

        [Parameter(ParameterSetName = "Registered", Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = "Direct")]
        public string Name { get; set; }

        /// <summary>
        /// Name of the computer to monitor events on.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Direct")]
        public string MachineName { get; set; }

        /// <summary>
        /// Name of the log to watch on the specified machine.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Direct")]
        public string LogName { get; set; }

        /// <summary>
        /// Array of event identifiers to monitor.
        /// </summary>
        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "Direct")]
        public int[] EventId { get; set; }

        /// <summary>
        /// Script block executed when matching events are detected.
        /// </summary>
        [Parameter(Mandatory = true, Position = 3, ParameterSetName = "Direct")]
        public ScriptBlock Action { get; set; }

        /// <summary>
        /// Number of threads used for event processing.
        /// </summary>
        [Parameter(ParameterSetName = "Direct")]
        [ValidateRange(1, 1024)]
        public int NumberOfThreads { get; set; } = 8;

        [Parameter(ParameterSetName = "Direct")]
        public int TimeoutSeconds { get; set; } = 0;

        [Parameter(ParameterSetName = "Direct")]
        public SwitchParameter StopOnMatch { get; set; }

        [Parameter(ParameterSetName = "Direct")]
        [ValidateRange(1, int.MaxValue)]
        public int StopAfter { get; set; } = 0;

        [Parameter(ParameterSetName = "Direct")]
        public ScriptBlock Until { get; set; }


        /// <summary>
        /// Initializes resources prior to processing.
        /// </summary>
        protected override Task BeginProcessingAsync() {
            _internalLogger = new InternalLogger(false);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Starts watching for events and invokes the provided action.
        /// </summary>
        private void StartWatcher(string runName, WatcherSettings settings) {
            WatcherRegistry.Start(runName, _internalLogger, settings, TimeoutSeconds, StopOnMatch, StopAfter, Until, CancelToken);
        }

        protected override Task ProcessRecordAsync() {
            WatcherSettings settings;
            string runName = Name;

            if (this.ParameterSetName == "Registered") {
                if (!WatcherRegistry.TryGet(Name, out var def)) {
                    WriteError(new ErrorRecord(new ItemNotFoundException($"Watcher '{Name}' not found"), "NotFound", ErrorCategory.ObjectNotFound, Name));
                    return Task.CompletedTask;
                }
                settings = def;
            } else {
                settings = new WatcherSettings {
                    Name = Name,
                    MachineName = MachineName,
                    LogName = LogName,
                    EventId = EventId,
                    Action = Action,
                    NumberOfThreads = NumberOfThreads
                };
            }

            if (string.IsNullOrEmpty(runName)) {
                runName = Guid.NewGuid().ToString();
            }

            StartWatcher(runName, settings);
            WriteObject(runName);
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
using EventViewerX;
using System.Management.Automation;
using System.Threading.Tasks;
using System.Linq;

namespace PSEventViewer {
    /// <summary>
    /// Starts real-time monitoring of Windows Event Logs with customizable filters and actions.
    /// Provides continuous event watching with scriptblock execution for matched events.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "EVXWatcher", DefaultParameterSetName = "EventId")]
    [Alias("Start-EventViewerXWatcher", "Start-EventWatching")]
    [OutputType(typeof(WatcherInfo))]
    public sealed class CmdletStartEVXWatcher : AsyncPSCmdlet {

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
        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "EventId")]
        public int[] EventId { get; set; }

        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "NamedEvent")]
        public NamedEvents[] NamedEvent { get; set; }

        /// <summary>
        /// Enables staging mode which also watches for event ID 350.
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Staging { get; set; }

        /// <summary>
        /// Script block executed when matching events are detected.
        /// </summary>
        [Parameter(Mandatory = true, Position = 3)]
        public ScriptBlock Action { get; set; }

        [Parameter]
        public string Name { get; set; }

        [Parameter]
        public TimeSpan? TimeOut { get; set; }

        [Parameter]
        public SwitchParameter StopOnMatch { get; set; }

        [Parameter]
        public int StopAfter { get; set; }

        /// <summary>
        /// Number of threads used for event processing.
        /// </summary>
        [Parameter(Mandatory = false)]
        [ValidateRange(1, 1024)]
        public int NumberOfThreads { get; set; } = 8;

        protected override Task ProcessRecordAsync() {
            var ids = new System.Collections.Generic.List<int>();
            if (ParameterSetName == "EventId" && EventId != null) {
                ids.AddRange(EventId);
            } else if (ParameterSetName == "NamedEvent" && NamedEvent != null) {
                var dict = EventObjectSlim.GetEventInfoForNamedEvents(NamedEvent.ToList());
                if (dict.TryGetValue(LogName, out var set)) {
                    ids.AddRange(set);
                } else {
                    WriteWarning($"No events found for named events in log {LogName}.");
                }
            }

            var watcher = WatcherManager.StartWatcher(
                Name,
                MachineName,
                LogName,
                ids,
                ParameterSetName == "NamedEvent" ? NamedEvent.ToList() : new System.Collections.Generic.List<NamedEvents>(),
                e => Action.Invoke(e),
                NumberOfThreads,
                Staging.IsPresent,
                StopOnMatch.IsPresent,
                StopAfter,
                TimeOut);
            WriteObject(watcher);
            return Task.CompletedTask;
        }
    }
}
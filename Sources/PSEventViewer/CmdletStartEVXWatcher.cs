using EventViewerX;
using System.Management.Automation;
using System.Threading.Tasks;
using System.Linq;

namespace PSEventViewer {
    /// <summary>
    /// <para type="synopsis">Starts real-time monitoring of Windows Event Logs with customizable filters and actions.</para>
    /// <para type="description">Supports explicit event IDs or NamedEvents, optional staging events, auto-stop conditions, multithreaded processing, and executes a script block for each match.</para>
    /// </summary>
    /// <example>
    ///   <summary>Watch security log for logon failures</summary>
    ///   <code>Start-EVXWatcher -MachineName DC1 -LogName Security -EventId 4625 -Action { Write-Host "Failed logon:" $_.MessageSubject }</code>
    ///   <para>Streams failed logons and prints a summary.</para>
    /// </example>
    /// <example>
    ///   <summary>Use NamedEvents for AD lockouts</summary>
    ///   <code>Start-EVXWatcher -MachineName DC1 -LogName Security -NamedEvent ADUserLockouts -Action { Send-MailMessage ... }</code>
    ///   <para>Triggers an alert when any AD lockout occurs.</para>
    /// </example>
    /// <example>
    ///   <summary>Stop after first hit</summary>
    ///   <code>Start-EVXWatcher -MachineName SRV1 -LogName System -EventId 41 -StopOnMatch -Action { $_ | Out-File crash.txt }</code>
    ///   <para>Captures the first critical kernel-power event then exits.</para>
    /// </example>
    /// <example>
    ///   <summary>Limit runtime</summary>
    ///   <code>Start-EVXWatcher -MachineName SRV1 -LogName Application -EventId 1000 -TimeOut (New-TimeSpan -Minutes 15) -Action { $_.WriteToHost() }</code>
    ///   <para>Watches for 15 minutes and then stops automatically.</para>
    /// </example>
    [Cmdlet(VerbsLifecycle.Start, "EVXWatcher", DefaultParameterSetName = "EventId")]
    [Alias("Start-EventViewerXWatcher", "Start-EventWatching")]
    [OutputType(typeof(WatcherInfo))]
    public sealed class CmdletStartEVXWatcher : AsyncPSCmdlet {

        /// <summary>
        /// Name of the computer to monitor events on.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string MachineName { get; set; } = null!;

        /// <summary>
        /// Name of the log to watch on the specified machine.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        public string LogName { get; set; } = null!;

        /// <summary>
        /// Array of event identifiers to monitor.
        /// </summary>
        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "EventId")]
        public int[] EventId { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Array of predefined event groups to monitor.
        /// </summary>
        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "NamedEvent")]
        public NamedEvents[] NamedEvent { get; set; } = Array.Empty<NamedEvents>();

        /// <summary>
        /// Enables staging mode which also watches for event ID 350.
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Staging { get; set; }

        /// <summary>
        /// Script block executed when matching events are detected.
        /// </summary>
        [Parameter(Mandatory = true, Position = 3)]
        public ScriptBlock Action { get; set; } = null!;

        /// <summary>
        /// Optional name for the watcher instance.
        /// </summary>
        [Parameter]
        public string? Name { get; set; }

        /// <summary>
        /// Duration after which the watcher stops automatically.
        /// </summary>
        [Parameter]
        public TimeSpan? TimeOut { get; set; }

        /// <summary>
        /// When set, the watcher stops after the first matching event.
        /// </summary>
        [Parameter]
        public SwitchParameter StopOnMatch { get; set; }

        /// <summary>
        /// Stops watching after processing the specified number of events.
        /// </summary>
        [Parameter]
        public int StopAfter { get; set; }

        /// <summary>
        /// Number of threads used for event processing.
        /// </summary>
        [Parameter(Mandatory = false)]
        [ValidateRange(1, 1024)]
        public int NumberOfThreads { get; set; } = 8;

        /// <summary>
        /// Starts the watcher based on provided filters and returns its information.
        /// </summary>
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
                ParameterSetName == "NamedEvent" ? (NamedEvent?.ToList() ?? new System.Collections.Generic.List<NamedEvents>()) : new System.Collections.Generic.List<NamedEvents>(),
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

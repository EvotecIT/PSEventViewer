using EventViewerX;
using System.Management.Automation;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Globalization;

namespace PSEventViewer {
    /// <summary>
    /// <para type="synopsis">Starts real-time monitoring of Windows Event Logs with customizable filters and actions.</para>
    /// <para type="description">Supports explicit event IDs or NamedEvents, provider-side filtering, optional staging events, auto-stop conditions, and a callback for each match.</para>
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
    [Cmdlet(
        VerbsLifecycle.Start,
        "EVXWatcher",
        DefaultParameterSetName = "EventId")]
    [OutputType(typeof(WatcherInfo))]
    public sealed class CmdletStartEVXWatcher : AsyncPSCmdlet {

        /// <summary>
        /// Optional computer to monitor. The local computer is used by default.
        /// </summary>
        [Parameter]
        public string? MachineName { get; set; }

        /// <summary>
        /// Name of the log to watch on the specified machine.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string LogName { get; set; } = null!;

        /// <summary>
        /// Array of event identifiers to monitor.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "EventId")]
        [ValidateRange(1, int.MaxValue)]
        public int[] EventId { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Array of predefined event groups to monitor.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "NamedEvent")]
        public NamedEvents[] NamedEvent { get; set; } = Array.Empty<NamedEvents>();

        /// <summary>
        /// Event predicates using the same keys as Get-EVXEvent -FilterHashtable.
        /// LogName and Path are not included because this watcher targets one LogName.
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 1,
            ParameterSetName = "FilterHashtable")]
        public Hashtable? FilterHashtable { get; set; }

        /// <summary>Native Windows Event Log XPath applied by the subscription.</summary>
        [Parameter(
            Mandatory = true,
            Position = 1,
            ParameterSetName = "FilterXPath")]
        public string? FilterXPath { get; set; }

        /// <summary>
        /// Enables staging mode which also watches for event ID 350.
        /// </summary>
        [Parameter(Mandatory = false, ParameterSetName = "EventId")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvent")]
        public SwitchParameter Staging { get; set; }

        /// <summary>Credentials used for a remote native subscription.</summary>
        [Credential]
        [Parameter]
        public PSCredential? Credential { get; set; }

        /// <summary>Authentication package used for a remote subscription.</summary>
        [Parameter]
        public EventLogAuthentication Authentication { get; set; }

        /// <summary>Future, Oldest, or AfterBookmark subscription starting position.</summary>
        [Parameter]
        public EventLogSubscriptionStart Start { get; set; } =
            EventLogSubscriptionStart.Future;

        /// <summary>Native bookmark XML used with Start=AfterBookmark.</summary>
        [Parameter]
        public string? BookmarkXml { get; set; }

        /// <summary>Allows a stale bookmark to resume from the closest available record.</summary>
        [Parameter]
        public SwitchParameter IgnoreStaleBookmark { get; set; }

        /// <summary>Allows Windows to tolerate query errors where the native API supports it.</summary>
        [Parameter]
        public SwitchParameter TolerateQueryErrors { get; set; }

        /// <summary>Amount of event data projected for every delivered event.</summary>
        [Parameter]
        public EventReadMode ReadMode { get; set; } = EventReadMode.Full;

        /// <summary>Primary culture for message and provider-label rendering.</summary>
        [Parameter]
        public CultureInfo? MessageCulture { get; set; } =
            CultureInfo.GetCultureInfo("en-US");

        /// <summary>Fallback culture when the primary provider resources are unavailable.</summary>
        [Parameter]
        public CultureInfo? FallbackMessageCulture { get; set; } =
            CultureInfo.CurrentUICulture;

        /// <summary>Maximum detached snapshots buffered before delivery stops rather than dropping data.</summary>
        [Parameter]
        [ValidateRange(1, 65536)]
        public int BufferCapacity { get; set; } = 256;

        /// <summary>Remote native session connection timeout in milliseconds.</summary>
        [Parameter]
        [ValidateRange(1, int.MaxValue)]
        public int SessionTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Script block executed when matching events are detected.
        /// </summary>
        [Parameter(Mandatory = true, Position = 2)]
        public ScriptBlock Action { get; set; } = null!;

        /// <summary>
        /// Optional name for the watcher instance.
        /// </summary>
        [Parameter]
        public string? Name { get; set; }

        /// <summary>
        /// Stable caller-defined identity used to reuse a named watcher across recreated host delegates.
        /// Omit this parameter to reject reuse when the action delegate is not the same instance.
        /// </summary>
        [Parameter]
        public string? ActionIdentity { get; set; }

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
        [ValidateRange(0, int.MaxValue)]
        public int StopAfter { get; set; }

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

            if ((ParameterSetName == "EventId" ||
                 ParameterSetName == "NamedEvent") &&
                ids.Count == 0) {
                throw new PSArgumentException($"No event IDs were resolved for log '{LogName}'.");
            }
            if (TimeOut.HasValue && TimeOut.Value <= TimeSpan.Zero) {
                throw new PSArgumentOutOfRangeException(nameof(TimeOut), TimeOut, "TimeOut must be greater than zero when provided.");
            }

            EventFilter? filter = ParameterSetName switch {
                "EventId" or "NamedEvent" => new EventFilter {
                    EventIds = Staging
                        ? ids
                            .Append(350)
                            .Distinct()
                            .OrderBy(static id => id)
                            .ToArray()
                        : ids
                            .Distinct()
                            .OrderBy(static id => id)
                            .ToArray()
                },
                "FilterHashtable" =>
                    PowerShellEventFilterAdapter.BindWatcherFilter(
                        FilterHashtable!),
                _ => null
            };
            if (filter?.ProviderNames?.Any(static provider =>
                    provider.IndexOf('*') >= 0 ||
                    provider.IndexOf('?') >= 0) == true) {
                var catalogQuery = new EventLogCatalogQuery {
                    MachineName = MachineName,
                    Credential = Credential?.GetNetworkCredential(),
                    Authentication = Authentication,
                    ConnectionTimeoutMilliseconds =
                        SessionTimeoutMs
                };
                string[] providerNames = EventLogCatalog
                    .GetProviderNames(
                        catalogQuery,
                        filter.ProviderNames,
                        CancelToken)
                    .ToArray();
                if (providerNames.Length == 0) {
                    throw new PSArgumentException(
                        "The watcher provider patterns did not match any registered provider.");
                }
                filter.ProviderNames = providerNames;
            }
            string[] xpaths = ParameterSetName == "FilterXPath"
                ? new[] { FilterXPath!.Trim() }
                : EventFilterPartitioner.Partition(filter!)
                    .Select(EventFilterCompiler.BuildXPath)
                    .ToArray();
            if (xpaths.Any(string.IsNullOrWhiteSpace)) {
                throw new PSArgumentException(
                    "FilterXPath cannot be empty or whitespace.");
            }
            if (Start == EventLogSubscriptionStart.AfterBookmark &&
                string.IsNullOrWhiteSpace(BookmarkXml)) {
                throw new PSArgumentException(
                    "Start=AfterBookmark requires BookmarkXml.");
            }
            if (Start != EventLogSubscriptionStart.AfterBookmark &&
                !string.IsNullOrWhiteSpace(BookmarkXml)) {
                throw new PSArgumentException(
                    "BookmarkXml requires Start=AfterBookmark.");
            }
            if (EventLogTarget.IsLocalMachine(MachineName) &&
                Credential != null) {
                throw new PSArgumentException(
                    "Credential can only be used with a remote MachineName.");
            }
            EventLogSubscriptionQuery[] queries = xpaths
                .Select(xpath =>
                    new EventLogSubscriptionQuery(LogName) {
                        MachineName = MachineName,
                        Credential = Credential?
                            .GetNetworkCredential(),
                        Authentication = Authentication,
                        XPath = xpath,
                        Start = Start,
                        BookmarkXml = BookmarkXml,
                        StrictBookmark = !IgnoreStaleBookmark,
                        TolerateQueryErrors =
                            TolerateQueryErrors,
                        ReadMode = ReadMode,
                        MessageCulture = MessageCulture,
                        FallbackMessageCulture =
                            FallbackMessageCulture,
                        BufferCapacity = BufferCapacity,
                        RemoteConnectionTimeoutMilliseconds =
                            SessionTimeoutMs
                    })
                .ToArray();

            var bridge = new PowerShellWatcherEventBridge();
            PSEventManager eventManager = Events;
            string sourceIdentifier = $"PSEventViewer.Watcher.{Guid.NewGuid():N}";
            PSEventSubscriber subscriber = eventManager.SubscribeEvent(
                bridge,
                nameof(PowerShellWatcherEventBridge.EventReceived),
                sourceIdentifier,
                PSObject.AsPSObject(Action),
                PowerShellWatcherEventBridge.ActionScript,
                supportEvent: false,
                forwardEvent: false);

            WatcherInfo? watcher = null;
            Action<EventObject> publish = bridge.Publish;
            Guid watcherOwnerId = PowerShellResourceOwnerId;
            EventHandler? stoppedHandler = null;
            bool createdPowerShellWatcher = false;
            int subscriptionRemoved = 0;
            void RemovePowerShellSubscription() {
                if (Interlocked.Exchange(ref subscriptionRemoved, 1) != 0) {
                    return;
                }

                if (watcher != null && stoppedHandler != null) {
                    watcher.Stopped -= stoppedHandler;
                }
                eventManager.UnsubscribeEvent(subscriber);
            }
            bridge.AttachCleanup(RemovePowerShellSubscription);

            try {
                watcher = WatcherManager.StartWatcher(
                    Name,
                    queries,
                    publish,
                    StopOnMatch.IsPresent,
                    StopAfter,
                    TimeOut,
                    string.IsNullOrWhiteSpace(ActionIdentity) ? null : ActionIdentity!.Trim(),
                    reuseScopeIdentity: watcherOwnerId.ToString("N"),
                    namedEvents: ParameterSetName == "NamedEvent"
                        ? NamedEvent?.ToList()
                        : null);
                createdPowerShellWatcher = watcher.Action.Equals(publish);
                if (!createdPowerShellWatcher) {
                    RemovePowerShellSubscription();
                } else {
                    PowerShellWatcherRegistry.Register(watcherOwnerId, watcher.Id);
                    stoppedHandler = (_, _) => {
                        PowerShellWatcherRegistry.Unregister(watcherOwnerId, watcher.Id);
                        bridge.RequestCleanup(
                            synchronousWhenIdle: true);
                    };
                    watcher.Stopped += stoppedHandler;
                    if (watcher.IsStopped) {
                        PowerShellWatcherRegistry.Unregister(watcherOwnerId, watcher.Id);
                        bridge.RequestCleanup(
                            synchronousWhenIdle: true);
                    }
                }
                WriteObject(watcher);
            } catch {
                RemovePowerShellSubscription();
                if (createdPowerShellWatcher && watcher != null) {
                    PowerShellWatcherRegistry.Unregister(watcherOwnerId, watcher.Id);
                    WatcherManager.StopWatcher(watcher.Id);
                }
                throw;
            }
            return Task.CompletedTask;
        }
    }
}

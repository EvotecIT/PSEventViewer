using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventViewerX {
    /// <summary>
    /// Represents information about a running watcher instance.
    /// </summary>
    public class WatcherInfo : IDisposable {
        internal WatcherInfo(
            string name,
            string machineName,
            string logName,
            List<int> eventIds,
            List<NamedEvents> namedEvents,
            Action<EventObject> action,
            bool staging,
            bool stopOnMatch,
            int stopAfter,
            TimeSpan? timeout)
            : this(
                name,
                machineName,
                logName,
                eventIds,
                namedEvents,
                action,
                staging,
                stopOnMatch,
                stopAfter,
                timeout,
                subscriptionQuery: null) {
        }

        internal WatcherInfo(
            string name,
            string machineName,
            string logName,
            List<int> eventIds,
            List<NamedEvents> namedEvents,
            Action<EventObject> action,
            bool staging,
            bool stopOnMatch,
            int stopAfter,
            TimeSpan? timeout,
            EventLogSubscriptionQuery? subscriptionQuery = null) {

            Name = name;
            MachineName = machineName;
            LogName = logName;
            EventIds = eventIds.ToArray();
            NamedEvents = namedEvents.ToArray();
            Action = action;
            Staging = staging;
            StopOnMatch = stopOnMatch;
            StopAfter = stopAfter;
            Timeout = timeout;
            _staging = staging;
            Watcher = new WatchEvents(new InternalLogger(false));
            Watcher.Stopped += OnWatcherStopped;
            SubscriptionQueries = new[] {
                subscriptionQuery == null
                    ? CreateSubscriptionQuery(
                        machineName,
                        logName,
                        eventIds,
                        staging)
                    : CloneSubscriptionQuery(subscriptionQuery)
            };
        }

        internal WatcherInfo(
            string name,
            string machineName,
            string logName,
            List<int> eventIds,
            List<NamedEvents> namedEvents,
            Action<EventObject> action,
            bool staging,
            bool stopOnMatch,
            int stopAfter,
            TimeSpan? timeout,
            IReadOnlyList<EventLogSubscriptionQuery> subscriptionQueries)
            : this(
                name,
                machineName,
                logName,
                eventIds,
                namedEvents,
                action,
                staging,
                stopOnMatch,
                stopAfter,
                timeout,
                subscriptionQuery: subscriptionQueries?.FirstOrDefault()) {

            if (subscriptionQueries == null ||
                subscriptionQueries.Count == 0) {
                throw new ArgumentException(
                    "At least one subscription query is required.",
                    nameof(subscriptionQueries));
            }
            SubscriptionQueries = subscriptionQueries
                .Select(CloneSubscriptionQuery)
                .ToArray();
        }

        private readonly bool _staging;
        private readonly object _stopSync = new();
        private bool _started;
        private bool _stopRequested;
        private bool _stopped;
        private int _eventsAccepted;
        private int _stopScheduled;
        private DateTime? _endTime;
        /// <summary>Unique identifier assigned to the watcher instance.</summary>
        public Guid Id { get; } = Guid.NewGuid();
        /// <summary>User-friendly name used to find and deduplicate watchers.</summary>
        public string Name { get; }
        /// <summary>Target computer name for the watcher.</summary>
        public string MachineName { get; }
        /// <summary>Event log name being monitored.</summary>
        public string LogName { get; }
        /// <summary>Event IDs the watcher listens for.</summary>
        public IReadOnlyList<int> EventIds { get; }
        /// <summary>NamedEvents packs that were expanded into <see cref="EventIds"/>.</summary>
        public IReadOnlyList<NamedEvents> NamedEvents { get; }
        /// <summary>Callback invoked when a matching event arrives.</summary>
        public Action<EventObject> Action { get; }
        internal string? ActionIdentity { get; set; }
        internal string? ReuseScopeIdentity { get; set; }
        /// <summary>Whether staging event ID 350 is included in the subscription.</summary>
        public bool Staging { get; }
        /// <summary>Stops the watcher after the first match when <c>true</c>.</summary>
        public bool StopOnMatch { get; }
        /// <summary>Optional cap on number of matching events before stopping.</summary>
        public int StopAfter { get; }
        /// <summary>Optional timeout after which the watcher is stopped automatically.</summary>
        public TimeSpan? Timeout { get; }
        internal CancellationTokenSource Cancellation { get; } = new();
        internal Task? TimeoutTask { get; private set; }
        /// <summary>Underlying watcher engine instance.</summary>
        public WatchEvents Watcher { get; }
        /// <summary>Complete native subscription contract owned by this watcher.</summary>
        public IReadOnlyList<EventLogSubscriptionQuery>
            SubscriptionQueries { get; }

        /// <summary>
        /// First native subscription contract. Use <see cref="SubscriptionQueries"/>
        /// when the watcher was partitioned across native XPath limits.
        /// </summary>
        public EventLogSubscriptionQuery SubscriptionQuery =>
            SubscriptionQueries[0];

        /// <summary>Total number of matching events accepted for delivery by this watcher.</summary>
        public int EventsFound => Volatile.Read(ref _eventsAccepted);
        /// <summary>UTC start time of the watcher.</summary>
        public DateTime StartTime => Watcher.StartTime;
        /// <summary>UTC stop time of the watcher if it has ended; otherwise <c>null</c>.</summary>
        public DateTime? EndTime {
            get {
                lock (_stopSync) {
                    return _endTime;
                }
            }
        }
        /// <summary>Whether this watcher has completed resource shutdown.</summary>
        public bool IsStopped {
            get {
                lock (_stopSync) {
                    return _stopped;
                }
            }
        }

        /// <summary>Begins monitoring and starts the optional timeout timer.</summary>
        public void Start() {
            lock (_stopSync) {
                if (_stopRequested ||
                    _stopped) {
                    throw new ObjectDisposedException(nameof(WatcherInfo));
                }
                if (_started) {
                    return;
                }

                Watcher.Watch(
                    SubscriptionQueries,
                    OnEvent,
                    Cancellation.Token);
                _started = true;
                if (Timeout.HasValue) {
                    TimeoutTask = StopAfterTimeoutAsync(Timeout.Value, Cancellation.Token);
                }
            }
        }

        private async Task StopAfterTimeoutAsync(TimeSpan timeout, CancellationToken cancellationToken) {
            try {
                await Task.Delay(timeout, cancellationToken).ConfigureAwait(false);
                Stop();
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            }
        }

        /// <summary>Invokes the user callback for each matching event and applies stop conditions.</summary>
        /// <param name="obj">Event object passed to the callback.</param>
        private void OnEvent(EventObject obj) {
            int acceptedEventNumber = TryAcceptEvent();
            if (acceptedEventNumber == 0) {
                return;
            }

            Exception? exCaught = null;
            try {
                Action?.Invoke(obj);
            } catch (Exception ex) {
                exCaught = ex;
                Settings._logger.WriteWarning("OnEvent callback threw: {0}", ex.Message.Trim());
            }

            if (StopOnMatch ||
                (StopAfter > 0 &&
                 acceptedEventNumber >= StopAfter)) {
                ScheduleStop();
            }

            if (exCaught != null) {
                ActionException?.Invoke(this, exCaught);
            }
        }

        private int TryAcceptEvent() {
            int deliveryLimit = StopOnMatch
                ? 1
                : StopAfter;
            if (deliveryLimit <= 0) {
                return Interlocked.Increment(
                    ref _eventsAccepted);
            }

            while (true) {
                int current = Volatile.Read(
                    ref _eventsAccepted);
                if (current >= deliveryLimit) {
                    return 0;
                }
                int next = current + 1;
                if (Interlocked.CompareExchange(
                        ref _eventsAccepted,
                        next,
                        current) == current) {
                    return next;
                }
            }
        }

        /// <summary>Raised when the user-supplied <see cref="Action"/> throws.</summary>
        public event EventHandler<Exception>? ActionException;

        /// <summary>Raised after the watcher has released its native resources.</summary>
        public event EventHandler? Stopped;

        private void ScheduleStop() {
            if (Interlocked.Exchange(ref _stopScheduled, 1) == 0) {
                _ = Task.Run(Stop);
            }
        }

        private void OnWatcherStopped(
            object? sender,
            EventArgs args) {

            ScheduleStop();
        }

        /// <summary>Stops the watcher, disposes resources, and records end time.</summary>
        public void Stop() {
            lock (_stopSync) {
                if (_stopRequested ||
                    _stopped) {
                    return;
                }
                _stopRequested = true;
            }

            try {
                Watcher.Stopped -= OnWatcherStopped;
                Cancellation.Cancel();
            } finally {
                try {
                    Watcher.Dispose();
                } finally {
                    Cancellation.Dispose();
                    lock (_stopSync) {
                        _endTime = DateTime.UtcNow;
                        _stopped = true;
                    }
                }
            }
            Stopped?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Stops the watcher and disposes internal cancellation token.</summary>
        public void Dispose() {
            Stop();
        }

        private static EventLogSubscriptionQuery CreateSubscriptionQuery(
            string machineName,
            string logName,
            IReadOnlyList<int> eventIds,
            bool staging) {

            var ids = new HashSet<int>(
                eventIds.Where(static id => id > 0));
            if (staging) {
                ids.Add(350);
            }
            return new EventLogSubscriptionQuery(logName) {
                MachineName = machineName,
                XPath = EventFilterCompiler.BuildXPath(
                    new EventFilter {
                        EventIds = ids
                            .OrderBy(static id => id)
                            .ToArray()
                    }),
                Start = EventLogSubscriptionStart.Future,
                ReadMode = EventReadMode.Full,
                RemoteConnectionTimeoutMilliseconds =
                    Settings.SessionTimeoutMs,
                BufferCapacity = 256
            };
        }

        private static EventLogSubscriptionQuery CloneSubscriptionQuery(
            EventLogSubscriptionQuery source) {

            return new EventLogSubscriptionQuery(source.LogName) {
                MachineName = source.MachineName,
                Credential = source.Credential,
                Authentication = source.Authentication,
                XPath = source.XPath,
                Start = source.Start,
                BookmarkXml = source.BookmarkXml,
                StrictBookmark = source.StrictBookmark,
                TolerateQueryErrors = source.TolerateQueryErrors,
                ReadMode = source.ReadMode,
                MessageCulture = source.MessageCulture,
                FallbackMessageCulture = source.FallbackMessageCulture,
                BufferCapacity = source.BufferCapacity,
                RemoteConnectionTimeoutMilliseconds =
                    source.RemoteConnectionTimeoutMilliseconds
            };
        }
    }

}

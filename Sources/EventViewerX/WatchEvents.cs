using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;

namespace EventViewerX {
    /// <summary>
    /// Watches an event log and invokes a callback for matching events.
    /// </summary>
    public class WatchEvents : Settings, IDisposable {
        private static int _numberOfEventsFound;
        private readonly object _lifecycleSync = new();
        private readonly InternalLogger _instanceLogger;
        private volatile HashSet<int> _watchEventIds = new();
        private EventLogSession? _eventLogSession;
        private EventLogWatcher? _eventLogWatcher;
        private CancellationTokenRegistration _cancellationRegistration;
        private bool _hasCancellationRegistration;
        private Action<EventObject>? _eventAction;
        private string? _machineName;
        private int _eventsFound;

        /// <summary>Total number of matching events observed by all watchers in this process.</summary>
        public static int NumberOfEventsFound => Volatile.Read(ref _numberOfEventsFound);

        /// <summary>Number of events captured during the current watch session.</summary>
        public int EventsFound => Volatile.Read(ref _eventsFound);

        /// <summary>Time when the current or most recent watch started.</summary>
        public DateTime StartTime { get; private set; }

        /// <summary>The most recent managed event snapshot observed by this watcher.</summary>
        public EventObject? LastEvent { get; private set; }

        /// <summary>Indicates whether staging event ID 350 is included.</summary>
        public bool StagingEnabled { get; private set; }

        /// <summary>Username of the account that enabled staging.</summary>
        public string? StagingEnabledBy { get; private set; }

        /// <summary>
        /// Initializes a new watcher.
        /// </summary>
        /// <param name="internalLogger">Optional logger used by watcher callbacks.</param>
        public WatchEvents(InternalLogger? internalLogger = null) {
            _instanceLogger = internalLogger ?? Settings._logger;
        }

        /// <summary>Resets the process-wide matching event counter.</summary>
        public static void ResetGlobalEventCount() {
            Interlocked.Exchange(ref _numberOfEventsFound, 0);
        }

        /// <summary>
        /// Starts watching for specified event IDs.
        /// </summary>
        /// <param name="machineName">Target machine name; null targets the local computer.</param>
        /// <param name="logName">Event log channel name.</param>
        /// <param name="eventId">Positive event identifiers to monitor.</param>
        /// <param name="eventAction">Callback invoked for each matching event.</param>
        /// <param name="cancellationToken">Cancellation token used to stop watching.</param>
        /// <param name="staging">Whether event ID 350 is also monitored.</param>
        /// <param name="enabledBy">Account that enabled staging.</param>
        public void Watch(
            string? machineName,
            string logName,
            List<int> eventId,
            Action<EventObject>? eventAction = null,
            CancellationToken cancellationToken = default,
            bool staging = false,
            string? enabledBy = null) {

            if (string.IsNullOrWhiteSpace(logName)) {
                throw new ArgumentException("Log name cannot be null or whitespace.", nameof(logName));
            }
            if (eventId == null) {
                throw new ArgumentNullException(nameof(eventId));
            }
            cancellationToken.ThrowIfCancellationRequested();

            var ids = new HashSet<int>(eventId.Where(static id => id > 0));
            if (staging) {
                ids.Add(350);
            }
            if (ids.Count == 0) {
                throw new ArgumentException("At least one positive event ID is required.", nameof(eventId));
            }

            lock (_lifecycleSync) {
                DisposeCore(disposeCancellationRegistration: true);
                _watchEventIds = ids;
                _eventAction = eventAction;
                _machineName = string.IsNullOrWhiteSpace(machineName) ? Environment.MachineName : machineName;
                _eventsFound = 0;
                LastEvent = null;
                StartTime = DateTime.UtcNow;
                StagingEnabled = staging;
                StagingEnabledBy = staging ? enabledBy ?? Environment.UserName : null;

                try {
                    EventLogSessionOpenResult sessionResult = SearchEvents.OpenSessionResult(machineName, Settings.SessionTimeoutMs, "WatchEvents", logName);
                    if (!sessionResult.Success || sessionResult.Session == null) {
                        throw new InvalidOperationException(sessionResult.ErrorMessage);
                    }

                    _eventLogSession = sessionResult.Session;
                    sessionResult.Session = null;
                    sessionResult.Dispose();

                    string xpath = "*[System[" + string.Join(" or ", ids.OrderBy(static id => id).Select(static id => $"EventID={id}")) + "]]";
                    var query = new EventLogQuery(logName, PathType.LogName, xpath) {
                        Session = _eventLogSession,
                        TolerateQueryErrors = false
                    };
                    _eventLogWatcher = new EventLogWatcher(query);
                    _eventLogWatcher.EventRecordWritten += DetectEventsLogCallback;
                    _eventLogWatcher.Enabled = true;
                    if (cancellationToken.CanBeCanceled) {
                        _cancellationRegistration = cancellationToken.Register(CancelWatch);
                        _hasCancellationRegistration = true;
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    string eventIds = string.Join(",", ids.OrderBy(static id => id).Select(static id => id.ToString()));
                    _instanceLogger.WriteVerbose("Created event log subscription to {0} for {1}.", _machineName ?? Environment.MachineName, eventIds);
                } catch {
                    DisposeCore(disposeCancellationRegistration: true);
                    throw;
                }
            }
        }

        private void DetectEventsLogCallback(object? sender, EventRecordWrittenEventArgs args) {
            EventRecord? record = args.EventRecord;
            if (record == null) {
                string error = args.EventException?.Message ?? "The event subscription callback returned no event record.";
                _instanceLogger.WriteWarning("Event log subscription callback failed: {0}", error);
                return;
            }

            try {
                HashSet<int> ids = _watchEventIds;
                if (!ids.Contains(record.Id)) {
                    return;
                }

                EventRecord ownedRecord = record;
                record = null;
                var eventObject = new EventObject(ownedRecord, _machineName ?? Environment.MachineName, EventReadMode.Full);
                LastEvent = eventObject;
                Interlocked.Increment(ref _numberOfEventsFound);
                Interlocked.Increment(ref _eventsFound);
                _instanceLogger.WriteVerbose("Found event id {0} on {1}.", eventObject.Id, eventObject.MachineName);

                try {
                    _eventAction?.Invoke(eventObject);
                } catch (Exception ex) {
                    _instanceLogger.WriteWarning("Event watcher callback threw: {0}", ex.Message.Trim());
                }
            } catch (Exception ex) {
                _instanceLogger.WriteWarning("Event log subscription callback failed: {0}", ex.Message.Trim());
            } finally {
                record?.Dispose();
            }
        }

        private void CancelWatch() {
            lock (_lifecycleSync) {
                DisposeCore(disposeCancellationRegistration: false);
            }
        }

        /// <summary>Stops watching and releases native watcher/session resources.</summary>
        public void Dispose() {
            lock (_lifecycleSync) {
                DisposeCore(disposeCancellationRegistration: true);
            }
        }

        private void DisposeCore(bool disposeCancellationRegistration) {
            if (disposeCancellationRegistration && _hasCancellationRegistration) {
                _cancellationRegistration.Dispose();
                _cancellationRegistration = default;
                _hasCancellationRegistration = false;
            }

            if (_eventLogWatcher != null) {
                _eventLogWatcher.EventRecordWritten -= DetectEventsLogCallback;
                _eventLogWatcher.Enabled = false;
                _eventLogWatcher.Dispose();
                _eventLogWatcher = null;
            }

            _eventLogSession?.Dispose();
            _eventLogSession = null;
            _watchEventIds = new HashSet<int>();
            _eventAction = null;
            StagingEnabled = false;
            StagingEnabledBy = null;
        }
    }
}

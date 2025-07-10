using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;

namespace EventViewerX {
    /// <summary>
    /// Watches event logs and invokes callbacks when matching events appear.
    /// </summary>
    public class WatchEvents : Settings, IDisposable {
        /// <summary>Global count of all events detected.</summary>
        public static volatile int NumberOfEventsFound = 0;
        private int _eventsFound;
        /// <summary>Number of events captured during the current watch session.</summary>
        public int EventsFound => _eventsFound;
        /// <summary>Time when event watching started.</summary>
        public DateTime StartTime { get; private set; }
        /// <summary>
        /// List of event IDs to watch for
        /// </summary>
        private ConcurrentBag<int> _watchEventId = new ConcurrentBag<int>();

        /// <summary>
        /// Indicates whether staging is enabled.
        /// </summary>
        public bool StagingEnabled { get; private set; }

        /// <summary>
        /// Username of the account that enabled staging.
        /// </summary>
        public string StagingEnabledBy { get; private set; }

        /// <summary>
        /// Action executed when an event matching the filter is detected.
        /// </summary>
        private Action<EventObject> _eventAction;

        /// <summary>
        /// Events keyed by record identifier
        /// </summary>
        readonly ConcurrentDictionary<long, EventObject> WatchedEvents = new ConcurrentDictionary<long, EventObject>();

        private EventLogSession _eventLogSession;
        private EventLogWatcher _eventLogWatcher;

        private string _machineName;

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchEvents"/> class.
        /// </summary>
        /// <param name="internalLogger">Optional logger for verbose output.</param>
        public WatchEvents(InternalLogger internalLogger = null) {
            if (internalLogger != null) {
                _logger = internalLogger;
            }
        }

        /// <summary>
        /// Starts watching for specified event IDs.
        /// </summary>
        /// <param name="machineName">Target machine name.</param>
        /// <param name="logName">Event log name.</param>
        /// <param name="eventId">Event identifiers to monitor.</param>
        /// <param name="eventAction">Callback when event is detected.</param>
        /// <param name="cancellationToken">Cancellation token to stop watching.</param>
        /// <param name="staging">Whether to use staging mode.</param>
        /// <param name="enabledBy">Account that enabled staging.</param>
        public void Watch(string machineName, string logName, List<int> eventId, Action<EventObject> eventAction = null, CancellationToken cancellationToken = default, bool staging = false, string enabledBy = null) {
            NumberOfEventsFound = 0;
            _eventsFound = 0;
            StartTime = DateTime.UtcNow;
            Dispose();
            _machineName = string.IsNullOrEmpty(machineName) ? Environment.MachineName : machineName;
            if (staging && !eventId.Contains(350)) {
                eventId.Add(350);
                StagingEnabled = true;
                StagingEnabledBy = enabledBy ?? Environment.UserName;
            } else {
                StagingEnabled = false;
                StagingEnabledBy = null;
            }
            _watchEventId = new ConcurrentBag<int>(eventId);
            _eventAction = eventAction;
            try {
                if (!string.IsNullOrEmpty(machineName)) {
                    _eventLogSession = new EventLogSession(machineName);
                }
                EventLogQuery query = new EventLogQuery(logName, PathType.LogName);
                if (_eventLogSession != null) {
                    query.Session = _eventLogSession;
                }
                _eventLogWatcher = new EventLogWatcher(query);
                _eventLogWatcher.EventRecordWritten += DetectEventsLogCallback;
                _eventLogWatcher.Enabled = true;
                cancellationToken.Register(() => Dispose());
                _logger.WriteVerbose("Created event log subscription to {0}.", machineName);
            } catch (Exception ex) {
                _logger.WriteWarning("Failed to create event log subscription to Target Machine {0}. Verify network connectivity, firewall settings, permissions, etc. Continuing on to next DC if applicable...  ({1})", machineName, ex.Message.Trim());
            }
        }
        private void DetectEventsLogCallback(object Object, EventRecordWrittenEventArgs Args) {
            try {
                if (Args.EventRecord == null) {
                    _logger.WriteWarning("Event log subscription callback was given invalid data. This shouldn't happen.");
                }
            } catch (Exception ex) {
                _logger.WriteWarning("Event log subscription callback threw: ({0})", ex.Message.Trim());
                return;
            }
            if (Args.EventRecord != null) {
                var Event = Args.EventRecord;
                if (_watchEventId.Contains(Event.Id)) {
                    Interlocked.Increment(ref NumberOfEventsFound);
                    Interlocked.Increment(ref _eventsFound);
                    _logger.WriteVerbose("Found event id {0} on {1}.", Event.Id, Event.MachineName);

                    var eventObject = new EventObject(Event, _machineName);
                    WatchedEvents.TryAdd(eventObject.RecordId ?? -1L, eventObject);
                    _eventAction?.Invoke(eventObject);
                }
            } else {
                _logger.WriteWarning("Event log subscription callback was given invalid data. This shouldn't happen.");
            }
        }

        /// <summary>
        /// Stops watching and cleans up resources.
        /// </summary>
        public void Dispose() {
            if (_eventLogWatcher != null) {
                _eventLogWatcher.EventRecordWritten -= DetectEventsLogCallback;
                _eventLogWatcher.Enabled = false;
                _eventLogWatcher.Dispose();
                _eventLogWatcher = null;
            }
            _watchEventId = new ConcurrentBag<int>();
            StagingEnabled = false;
            StagingEnabledBy = null;
            _eventLogSession?.Dispose();
            _eventLogSession = null;
            StartTime = default;
        }
    }
}
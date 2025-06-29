using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;

namespace EventViewerX {
    public class WatchEvents : Settings {
        public static volatile int NumberOfEventsFound = 0;
        /// <summary>
        /// List of event IDs to watch for
        /// </summary>
        private ConcurrentBag<int> _watchEventId = new ConcurrentBag<int>();

        /// <summary>
        /// Action executed when an event matching the filter is detected.
        /// </summary>
        private Action<EventObject> _eventAction;

        /// <summary>
        /// Events
        /// </summary>
        readonly ConcurrentDictionary<EventObject, byte> WatchedEvents = new ConcurrentDictionary<EventObject, byte>();

        private string _machineName;

        public WatchEvents(InternalLogger internalLogger = null) {
            if (internalLogger != null) {
                _logger = internalLogger;
            }
        }

        public void Watch(string machineName, string logName, List<int> eventId, Action<EventObject> eventAction = null, CancellationToken cancellationToken = default) {
            _machineName = machineName;
            _watchEventId = new ConcurrentBag<int>(eventId);
            _eventAction = eventAction;
            try {
                var eventLogSession = new EventLogSession(machineName);
                var eventLogWatcher = new EventLogWatcher(new EventLogQuery(logName, PathType.LogName) {
                    Session = eventLogSession
                });
                eventLogWatcher.EventRecordWritten += DetectEventsLogCallback;
                eventLogWatcher.Enabled = true;
                _logger.WriteVerbose("Created event log subscription to {0}.", machineName);

                if (cancellationToken != CancellationToken.None) {
                    cancellationToken.Register(() => {
                        eventLogWatcher.Enabled = false;
                        eventLogWatcher.Dispose();
                        eventLogSession.Dispose();
                    });
                }
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
                    _logger.WriteVerbose("Found event id {0} on {1}.", Event.Id, Event.MachineName);

                    var eventObject = new EventObject(Event, _machineName);
                    WatchedEvents.TryAdd(eventObject, 0);
                    _eventAction?.Invoke(eventObject);
                }
            } else {
                _logger.WriteWarning("Event log subscription callback was given invalid data. This shouldn't happen.");
            }
        }
    }
}
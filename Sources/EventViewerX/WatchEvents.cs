﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;

namespace EventViewerX {
    public class WatchEvents : Settings, IDisposable {
        public static volatile int NumberOfEventsFound = 0;
        private int _eventsFound;
        public int EventsFound => _eventsFound;
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
        /// Events
        /// </summary>
        readonly ConcurrentDictionary<EventObject, byte> WatchedEvents = new ConcurrentDictionary<EventObject, byte>();

        private EventLogSession _eventLogSession;
        private EventLogWatcher _eventLogWatcher;

        private string _machineName;

        public WatchEvents(InternalLogger internalLogger = null) {
            if (internalLogger != null) {
                _logger = internalLogger;
            }
        }

        public void Watch(string machineName, string logName, List<int> eventId, Action<EventObject> eventAction = null, CancellationToken cancellationToken = default, bool staging = false, string enabledBy = null) {
            NumberOfEventsFound = 0;
            _eventsFound = 0;
            StartTime = DateTime.UtcNow;
            Dispose();
            _machineName = machineName;
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
                _eventLogSession = new EventLogSession(machineName);
                _eventLogWatcher = new EventLogWatcher(new EventLogQuery(logName, PathType.LogName) {
                    Session = _eventLogSession
                });
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
                    WatchedEvents.TryAdd(eventObject, 0);
                    _eventAction?.Invoke(eventObject);
                }
            } else {
                _logger.WriteWarning("Event log subscription callback was given invalid data. This shouldn't happen.");
            }
        }

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
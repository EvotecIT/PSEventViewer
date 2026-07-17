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
        internal WatcherInfo(string name, string machineName, string logName, List<int> eventIds, List<NamedEvents> namedEvents, Action<EventObject> action, bool staging, bool stopOnMatch, int stopAfter, TimeSpan? timeout) {
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
        }

        private readonly bool _staging;
        private readonly object _stopSync = new();
        private bool _started;
        private bool _stopped;
        private bool _cancellationDisposed;
        private int _stopScheduled;
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

        /// <summary>Total number of matched events observed by this watcher.</summary>
        public int EventsFound => Watcher.EventsFound;
        /// <summary>UTC start time of the watcher.</summary>
        public DateTime StartTime => Watcher.StartTime;
        /// <summary>UTC stop time of the watcher if it has ended; otherwise <c>null</c>.</summary>
        public DateTime? EndTime { get; private set; }

        /// <summary>Begins monitoring and starts the optional timeout timer.</summary>
        public void Start() {
            lock (_stopSync) {
                if (_stopped) {
                    throw new ObjectDisposedException(nameof(WatcherInfo));
                }
                if (_started) {
                    return;
                }

                Watcher.Watch(MachineName, LogName, new List<int>(EventIds), OnEvent, Cancellation.Token, _staging, Environment.UserName);
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
            Exception? exCaught = null;
            try {
                Action?.Invoke(obj);
            } catch (Exception ex) {
                exCaught = ex;
                Settings._logger.WriteWarning("OnEvent callback threw: {0}", ex.Message.Trim());
            }

            if (StopOnMatch) {
                ScheduleStop();
            } else if (StopAfter > 0 && Watcher.EventsFound >= StopAfter) {
                ScheduleStop();
            }

            if (exCaught != null) {
                ActionException?.Invoke(this, exCaught);
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

        /// <summary>Stops the watcher, disposes resources, and records end time.</summary>
        public void Stop() {
            bool stoppedNow = false;
            lock (_stopSync) {
                if (_stopped) {
                    return;
                }
                _stopped = true;
                Cancellation.Cancel();
                Watcher.Dispose();
                Cancellation.Dispose();
                _cancellationDisposed = true;
                EndTime = DateTime.UtcNow;
                stoppedNow = true;
            }
            if (stoppedNow) {
                Stopped?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Stops the watcher and disposes internal cancellation token.</summary>
        public void Dispose() {
            Stop();
            lock (_stopSync) {
                if (!_cancellationDisposed) {
                    Cancellation.Dispose();
                    _cancellationDisposed = true;
                }
            }
        }
    }

    /// <summary>Manages active watcher instances.</summary>
    public static class WatcherManager {
        private static readonly ConcurrentDictionary<Guid, WatcherInfo> _watchers = new();
        private static readonly ConcurrentDictionary<string, WatcherInfo> _watchersByName = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _syncRoot = new();

        /// <summary>
        /// Starts a watcher for the given machine/log and returns the tracking object.
        /// If a running watcher with the same non-empty name and identical configuration exists, that instance is returned.
        /// Reusing a name for different behavior is rejected rather than silently returning the wrong watcher.
        /// </summary>
        /// <param name="name">Optional friendly name used for reuse and lookup.</param>
        /// <param name="machineName">Target computer.</param>
        /// <param name="logName">Log name to monitor.</param>
        /// <param name="eventIds">Event IDs to watch.</param>
        /// <param name="namedEvents">NamedEvents packs expanded for discovery.</param>
        /// <param name="action">Callback invoked for each matching event.</param>
        /// <param name="staging">When true, also watches staging events (e.g., 350).</param>
        /// <param name="stopOnMatch">Stop after first match when true.</param>
        /// <param name="stopAfter">Stop after this many matches when &gt; 0.</param>
        /// <param name="timeout">Optional timeout after which the watcher stops.</param>
        /// <param name="actionIdentity">Optional stable callback identity used by hosts that recreate equivalent delegate instances.</param>
        /// <returns>A <see cref="WatcherInfo"/> describing the running watcher.</returns>
        public static WatcherInfo StartWatcher(string? name, string machineName, string logName, List<int> eventIds, List<NamedEvents> namedEvents, Action<EventObject> action, bool staging, bool stopOnMatch, int stopAfter, TimeSpan? timeout, string? actionIdentity = null) {
            if (string.IsNullOrWhiteSpace(machineName)) {
                machineName = Environment.MachineName;
            } else {
                machineName = machineName.Trim();
            }
            if (string.IsNullOrWhiteSpace(logName)) {
                throw new ArgumentException("Log name cannot be null or whitespace.", nameof(logName));
            }
            if (eventIds is null) {
                throw new ArgumentNullException(nameof(eventIds));
            }
            if (namedEvents is null) {
                throw new ArgumentNullException(nameof(namedEvents));
            }
            if (action is null) {
                throw new ArgumentNullException(nameof(action));
            }
            if (stopAfter < 0) {
                throw new ArgumentOutOfRangeException(nameof(stopAfter), "Stop-after count cannot be negative.");
            }
            if (timeout.HasValue && timeout.Value <= TimeSpan.Zero) {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive when provided.");
            }

            logName = logName.Trim();
            name = string.IsNullOrWhiteSpace(name) ? null : name!.Trim();
            eventIds = eventIds.Where(static id => id > 0).Distinct().OrderBy(static id => id).ToList();
            if (eventIds.Count == 0) {
                throw new ArgumentException("At least one positive event ID is required.", nameof(eventIds));
            }
            namedEvents = namedEvents.Distinct().OrderBy(static value => value).ToList();

            WatcherInfo info;
            lock (_syncRoot) {
                if (!string.IsNullOrEmpty(name)) {
                    if (_watchersByName.TryGetValue(name!, out var existingByName) && existingByName.EndTime == null) {
                        if (HasEquivalentConfiguration(existingByName, machineName, logName, eventIds, namedEvents, action, staging, stopOnMatch, stopAfter, timeout, actionIdentity)) {
                            return existingByName;
                        }

                        throw new InvalidOperationException($"A running watcher named '{name}' already exists with different configuration.");
                    }

                    // Detect pre-existing duplicates injected outside the manager.
                    var sameName = _watchers.Values.Where(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (sameName.Count > 1 && !_watchersByName.ContainsKey(name!)) {
                        throw new InvalidOperationException($"Multiple watchers with name '{name}' already exist.");
                    }
                    if (sameName.Count >= 1) {
                        var active = sameName.FirstOrDefault(w => w.EndTime == null) ?? sameName[0];
                        _watchersByName[name!] = active;
                        if (active.EndTime == null) {
                            return active;
                        }
                    }
                }

                info = new WatcherInfo(name ?? string.Empty, machineName, logName, eventIds, namedEvents, action, staging, stopOnMatch, stopAfter, timeout) {
                    ActionIdentity = actionIdentity
                };
                info.Stopped += RemoveStoppedWatcher;
                _watchers.TryAdd(info.Id, info);
                if (!string.IsNullOrEmpty(name)) {
                    _watchersByName[name!] = info;
                }
                try {
                    info.Start();
                    return info;
                } catch {
                    _watchers.TryRemove(info.Id, out _);
                    if (!string.IsNullOrEmpty(info.Name)) {
                        _watchersByName.TryRemove(info.Name, out _);
                    }
                    info.Dispose();
                    throw;
                }
            }
        }

        private static bool HasEquivalentConfiguration(
            WatcherInfo existing,
            string machineName,
            string logName,
            IReadOnlyList<int> eventIds,
            IReadOnlyList<NamedEvents> namedEvents,
            Action<EventObject> action,
            bool staging,
            bool stopOnMatch,
            int stopAfter,
            TimeSpan? timeout,
            string? actionIdentity) {

            bool actionMatches = existing.ActionIdentity != null || actionIdentity != null
                ? string.Equals(existing.ActionIdentity, actionIdentity, StringComparison.Ordinal)
                : existing.Action.Equals(action);
            return string.Equals(existing.MachineName, machineName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(existing.LogName, logName, StringComparison.OrdinalIgnoreCase) &&
                   existing.EventIds.SequenceEqual(eventIds) &&
                   existing.NamedEvents.SequenceEqual(namedEvents) &&
                   actionMatches &&
                   existing.Staging == staging &&
                   existing.StopOnMatch == stopOnMatch &&
                   existing.StopAfter == stopAfter &&
                   existing.Timeout == timeout;
        }

        private static void RemoveStoppedWatcher(object? sender, EventArgs args) {
            if (sender is not WatcherInfo info) {
                return;
            }

            _watchers.TryRemove(info.Id, out _);
            if (!string.IsNullOrEmpty(info.Name) &&
                _watchersByName.TryGetValue(info.Name, out WatcherInfo? mapped) &&
                ReferenceEquals(mapped, info)) {
                _watchersByName.TryRemove(info.Name, out _);
            }
        }

        /// <summary>Returns all active watchers or those matching a specific name.</summary>
        public static IReadOnlyCollection<WatcherInfo> GetWatchers(string? name = null) {
            if (string.IsNullOrEmpty(name)) {
                return _watchers.Values.ToList();
            }
            return _watchers.Values.Where(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>Stops and removes a watcher by its identifier.</summary>
        /// <returns><c>true</c> when a watcher was stopped; otherwise <c>false</c>.</returns>
        public static bool StopWatcher(Guid id) {
            if (_watchers.TryRemove(id, out var info)) {
                info.Dispose();
                // Remove name mapping if it points to this instance or if no watcher with that name exists
                if (!string.IsNullOrEmpty(info.Name)) {
                    _watchersByName.TryGetValue(info.Name, out var mapped);
                    if (mapped == null || ReferenceEquals(mapped, info)) {
                        _watchersByName.TryRemove(info.Name, out _);
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>Stops all watchers that share the given name.</summary>
        public static void StopWatchersByName(string name) {
            foreach (var w in GetWatchers(name)) {
                StopWatcher(w.Id);
            }
            _watchersByName.TryRemove(name, out _);
        }

        /// <summary>Stops every active watcher and clears internal tracking.</summary>
        public static void StopAll() {
            foreach (var id in _watchers.Keys.ToList()) {
                StopWatcher(id);
            }
            _watchersByName.Clear();
        }
    }
}

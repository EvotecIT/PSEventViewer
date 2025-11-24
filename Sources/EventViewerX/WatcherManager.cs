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
        internal WatcherInfo(string name, string machineName, string logName, List<int> eventIds, List<NamedEvents> namedEvents, Action<EventObject> action, int numberOfThreads, bool staging, bool stopOnMatch, int stopAfter, TimeSpan? timeout) {
            Name = name;
            MachineName = machineName;
            LogName = logName;
            EventIds = eventIds;
            NamedEvents = namedEvents;
            Action = action;
            StopOnMatch = stopOnMatch;
            StopAfter = stopAfter;
            Timeout = timeout;
            _staging = staging;
            Watcher = new WatchEvents(new InternalLogger(false)) { NumberOfThreads = numberOfThreads };
        }

        private readonly bool _staging;
        /// <summary>Unique identifier assigned to the watcher instance.</summary>
        public Guid Id { get; } = Guid.NewGuid();
        /// <summary>User-friendly name used to find and deduplicate watchers.</summary>
        public string Name { get; }
        /// <summary>Target computer name for the watcher.</summary>
        public string MachineName { get; }
        /// <summary>Event log name being monitored.</summary>
        public string LogName { get; }
        /// <summary>Event IDs the watcher listens for.</summary>
        public List<int> EventIds { get; }
        /// <summary>NamedEvents packs that were expanded into <see cref="EventIds"/>.</summary>
        public List<NamedEvents> NamedEvents { get; }
        /// <summary>Callback invoked when a matching event arrives.</summary>
        public Action<EventObject> Action { get; }
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
            Watcher.Watch(MachineName, LogName, EventIds, OnEvent, Cancellation.Token, _staging, Environment.UserName);
            if (Timeout.HasValue) {
                var delayMs = (int)Timeout.Value.TotalMilliseconds;
                TimeoutTask = Task.Run(async () => {
                    try {
                        await Task.Delay(delayMs, Cancellation.Token);
                    } catch (TaskCanceledException) {
                    } finally {
                        if (EndTime == null) {
                            Stop();
                        }
                    }
                });
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
                Stop();
            } else if (StopAfter > 0 && Watcher.EventsFound >= StopAfter) {
                Stop();
            }

            if (exCaught != null) {
                ActionException?.Invoke(this, exCaught);
            }
        }

        /// <summary>Raised when the user-supplied <see cref="Action"/> throws.</summary>
        public event EventHandler<Exception>? ActionException;

        /// <summary>Stops the watcher, disposes resources, and records end time.</summary>
        public void Stop() {
            Cancellation.Cancel();
            Watcher.Dispose();
            EndTime = DateTime.UtcNow;
        }

        /// <summary>Stops the watcher and disposes internal cancellation token.</summary>
        public void Dispose() {
            Stop();
            Cancellation.Dispose();
        }
    }

    /// <summary>Manages active watcher instances.</summary>
    public static class WatcherManager {
        private static readonly ConcurrentDictionary<Guid, WatcherInfo> _watchers = new();
        private static readonly ConcurrentDictionary<string, WatcherInfo> _watchersByName = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _syncRoot = new();

        /// <summary>
        /// Starts (or reuses) a watcher for the given machine/log and returns the tracking object.
        /// If a running watcher with the same non-empty name exists, that instance is returned instead of creating a duplicate.
        /// </summary>
        /// <param name="name">Optional friendly name used for reuse and lookup.</param>
        /// <param name="machineName">Target computer.</param>
        /// <param name="logName">Log name to monitor.</param>
        /// <param name="eventIds">Event IDs to watch.</param>
        /// <param name="namedEvents">NamedEvents packs expanded for discovery.</param>
        /// <param name="action">Callback invoked for each matching event.</param>
        /// <param name="numberOfThreads">Worker threads to use inside the watcher.</param>
        /// <param name="staging">When true, also watches staging events (e.g., 350).</param>
        /// <param name="stopOnMatch">Stop after first match when true.</param>
        /// <param name="stopAfter">Stop after this many matches when &gt; 0.</param>
        /// <param name="timeout">Optional timeout after which the watcher stops.</param>
        /// <returns>A <see cref="WatcherInfo"/> describing the running watcher.</returns>
        public static WatcherInfo StartWatcher(string? name, string machineName, string logName, List<int> eventIds, List<NamedEvents> namedEvents, Action<EventObject> action, int numberOfThreads, bool staging, bool stopOnMatch, int stopAfter, TimeSpan? timeout) {
            WatcherInfo info;
            lock (_syncRoot) {
                // If external code injected duplicates directly into the backing store without name mapping, fail fast.
                if (_watchersByName.IsEmpty && _watchers.Count > 1) {
                    throw new InvalidOperationException("Multiple watchers already exist without name mapping.");
                }

                if (!string.IsNullOrEmpty(name)) {
                    // Fast reuse when a live watcher with the same name exists.
                    if (_watchersByName.TryGetValue(name!, out var existingByName) && existingByName.EndTime == null) {
                        return existingByName;
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

                info = new WatcherInfo(name ?? string.Empty, machineName, logName, eventIds, namedEvents, action, numberOfThreads, staging, stopOnMatch, stopAfter, timeout);
                _watchers.TryAdd(info.Id, info);
                if (!string.IsNullOrEmpty(name)) {
                    _watchersByName[name!] = info;
                }
            }

            info.Start();
            return info;
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

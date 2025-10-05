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
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; }
        public string MachineName { get; }
        public string LogName { get; }
        public List<int> EventIds { get; }
        public List<NamedEvents> NamedEvents { get; }
        public Action<EventObject> Action { get; }
        public bool StopOnMatch { get; }
        public int StopAfter { get; }
        public TimeSpan? Timeout { get; }
        internal CancellationTokenSource Cancellation { get; } = new();
        internal Task? TimeoutTask { get; private set; }
        public WatchEvents Watcher { get; }

        public int EventsFound => Watcher.EventsFound;
        public DateTime StartTime => Watcher.StartTime;
        public DateTime? EndTime { get; private set; }

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

        /// <summary>
        /// Invokes the user provided callback when a matching event is detected.
        /// </summary>
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

        /// <summary>
        /// Occurs when the callback passed to <see cref="StartWatcher"/> throws an exception.
        /// </summary>
        public event EventHandler<Exception>? ActionException;

        public void Stop() {
            Cancellation.Cancel();
            Watcher.Dispose();
            EndTime = DateTime.UtcNow;
        }

        public void Dispose() {
            Stop();
            Cancellation.Dispose();
        }
    }

    /// <summary>
    /// Manages active watcher instances.
    /// </summary>
    public static class WatcherManager {
        private static readonly ConcurrentDictionary<Guid, WatcherInfo> _watchers = new();
        private static readonly ConcurrentDictionary<string, WatcherInfo> _watchersByName = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _syncRoot = new();

        public static WatcherInfo StartWatcher(string? name, string machineName, string logName, List<int> eventIds, List<NamedEvents> namedEvents, Action<EventObject> action, int numberOfThreads, bool staging, bool stopOnMatch, int stopAfter, TimeSpan? timeout) {
            WatcherInfo info;
            lock (_syncRoot) {
                if (!string.IsNullOrEmpty(name)) {
                    // Fast path: direct name lookup; drop stale (ended) instances
                    if (_watchersByName.TryGetValue(name!, out var existing)) {
                        if (existing != null && existing.EndTime == null) {
                            return existing;
                        }
                        // Stale entry: remove and fall through to create a fresh one
                        _watchersByName.TryRemove(name!, out _);
                        var stale = _watchers.Values.Where(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
                        foreach (var s in stale) {
                            _watchers.TryRemove(s.Id, out _);
                        }
                    }

                    // Defensive: verify dictionary consistency and duplicate detection
                    var matches = _watchers.Values.Where(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase) && w.EndTime == null).ToList();
                    if (matches.Count == 1) {
                        _watchersByName[name!] = matches[0];
                        return matches[0];
                    }
                    if (matches.Count > 1) {
                        throw new InvalidOperationException($"Multiple watchers with name '{name}' already exist.");
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

        public static IReadOnlyCollection<WatcherInfo> GetWatchers(string? name = null) {
            if (string.IsNullOrEmpty(name)) {
                return _watchers.Values.ToList();
            }
            return _watchers.Values.Where(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        }

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

        public static void StopWatchersByName(string name) {
            foreach (var w in GetWatchers(name)) {
                StopWatcher(w.Id);
            }
            _watchersByName.TryRemove(name, out _);
        }

        public static void StopAll() {
            foreach (var id in _watchers.Keys.ToList()) {
                StopWatcher(id);
            }
            _watchersByName.Clear();
        }
    }
}

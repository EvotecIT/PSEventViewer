using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Linq;
using System.Threading;
using EventViewerX;

namespace PSEventViewer {
    /// <summary>
    /// Settings describing a watcher definition stored in the registry.
    /// </summary>
    public class WatcherSettings {
        public string Name { get; set; }
        public string MachineName { get; set; }
        public string LogName { get; set; }
        public int[] EventId { get; set; }
        public ScriptBlock Action { get; set; }
        public int NumberOfThreads { get; set; } = 8;
    }

    /// <summary>
    /// Runtime information about a running watcher instance.
    /// </summary>
    public class RunningWatcherInfo {
        public WatcherSettings Definition { get; set; }
        public CancellationTokenSource Cancellation { get; set; }
        public DateTime Started { get; set; }
    }

    /// <summary>
    /// Global registry for watcher definitions and running instances.
    /// </summary>
    public static class WatcherRegistry {
        private static readonly ConcurrentDictionary<string, WatcherSettings> Definitions = new();
        private static readonly ConcurrentDictionary<string, RunningWatcherInfo> Running = new();

        public static void Register(WatcherSettings def) {
            if (string.IsNullOrEmpty(def?.Name)) {
                throw new ArgumentException("Watcher name must be provided", nameof(def));
            }
            Definitions[def.Name] = def;
        }

        public static bool TryGet(string name, out WatcherSettings def) => Definitions.TryGetValue(name, out def);

        public static IEnumerable<WatcherSettings> GetAllDefinitions() => Definitions.Values;

        public static bool RemoveDefinition(string name) => Definitions.TryRemove(name, out _);

        public static IReadOnlyDictionary<string, RunningWatcherInfo> GetRunning() => Running;

        public static bool Stop(string name) {
            if (Running.TryRemove(name, out var info)) {
                info.Cancellation.Cancel();
                return true;
            }
            return false;
        }

        public static string Start(string name, InternalLogger logger, WatcherSettings def, int timeoutSeconds, bool stopOnMatch, int stopAfter, ScriptBlock until, CancellationToken token) {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            if (timeoutSeconds > 0) {
                cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            }

            int matches = 0;
            var watcher = new WatchEvents(logger) {
                NumberOfThreads = def.NumberOfThreads
            };

            watcher.Watch(def.MachineName, def.LogName, def.EventId.ToList(), e => {
                def.Action.Invoke(e);
                bool stop = false;
                if (until != null) {
                    object result = until.InvokeReturnAsIs(e);
                    if (LanguagePrimitives.IsTrue(result)) {
                        stop = true;
                    }
                }
                if (stopOnMatch) {
                    stop = true;
                }
                if (stopAfter > 0 && Interlocked.Increment(ref matches) >= stopAfter) {
                    stop = true;
                }
                if (stop) {
                    cts.Cancel();
                }
            }, cts.Token);

            cts.Token.Register(() => watcher.Dispose());

            var info = new RunningWatcherInfo {
                Definition = def,
                Cancellation = cts,
                Started = DateTime.UtcNow
            };
            Running[name] = info;
            return name;
        }
    }
}

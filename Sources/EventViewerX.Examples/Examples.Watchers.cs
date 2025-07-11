using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EventViewerX.Examples {
    internal partial class Examples {
        public static void WatchBasic() {
            var watcher = WatcherManager.StartWatcher(
                "basic",
                Environment.MachineName,
                "Security",
                new List<int> { 4624, 4625 },
                new List<NamedEvents>(),
                e => Console.WriteLine($"Event {e.Id} arrived"),
                4,
                false,
                false,
                0,
                null);
            Thread.Sleep(TimeSpan.FromSeconds(30));
            Console.WriteLine($"Events found: {watcher.EventsFound}");
            WatcherManager.StopAll();
        }

        public static void WatchNamedEvents() {
            var watcher = WatcherManager.StartWatcher(
                "named",
                Environment.MachineName,
                "System",
                EventObjectSlim.GetEventInfoForNamedEvents(new List<NamedEvents> { NamedEvents.OSCrash })["System"].ToList(),
                new List<NamedEvents> { NamedEvents.OSCrash },
                e => Console.WriteLine($"Named event {e.Id}"),
                4,
                false,
                false,
                0,
                null);
            Thread.Sleep(TimeSpan.FromSeconds(30));
            WatcherManager.StopWatcher(watcher.Id);
        }

        public static void WatchWithStopAfter() {
            var watcher = WatcherManager.StartWatcher(
                "stopAfter",
                Environment.MachineName,
                "Security",
                new List<int> { 4625 },
                new List<NamedEvents>(),
                e => Console.WriteLine("Event " + e.Id),
                4,
                false,
                false,
                2,
                null);
            while (WatcherManager.GetWatchers("stopAfter").Any()) {
                Thread.Sleep(1000);
            }
        }

        public static void WatchWithTimeout() {
            var watcher = WatcherManager.StartWatcher(
                "timeout",
                Environment.MachineName,
                "Application",
                new List<int> { 1000 },
                new List<NamedEvents>(),
                e => Console.WriteLine("Event " + e.Id),
                4,
                false,
                false,
                0,
                TimeSpan.FromSeconds(10));
            Thread.Sleep(TimeSpan.FromSeconds(15));
            Console.WriteLine($"Ended at {watcher.EndTime}");
        }

        public static void WatchDuplicateName() {
            var first = WatcherManager.StartWatcher(
                "duplicate",
                Environment.MachineName,
                "Application",
                new List<int> { 1 },
                new List<NamedEvents>(),
                _ => { },
                1,
                false,
                false,
                0,
                null);
            var second = WatcherManager.StartWatcher(
                "duplicate",
                Environment.MachineName,
                "Application",
                new List<int> { 1 },
                new List<NamedEvents>(),
                _ => { },
                1,
                false,
                false,
                0,
                null);
            Console.WriteLine(first == second ? "Same watcher" : "Different watcher");
            WatcherManager.StopAll();
        }
    }
}

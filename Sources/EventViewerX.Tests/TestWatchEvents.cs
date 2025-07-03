using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace EventViewerX.Tests {
    public class TestWatchEvents {
        private static ConcurrentBag<int> GetIds(WatchEvents watcher) {
            var field = typeof(WatchEvents).GetField("_watchEventId", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return (ConcurrentBag<int>)field!.GetValue(watcher)!;
        }

        [Fact]
        public void DisposeClearsWatchEventIds() {
            var watcher = new WatchEvents();
            watcher.Watch(Environment.MachineName, "Application", new List<int> { 1 });
            watcher.Dispose();
            var ids = GetIds(watcher);
            Assert.Empty(ids);
        }

        [Fact]
        public void SubsequentWatchesUseNewIdsOnly() {
            var watcher = new WatchEvents();
            watcher.Watch(Environment.MachineName, "Application", new List<int> { 1 });
            watcher.Watch(Environment.MachineName, "Application", new List<int> { 2 });
            var ids = GetIds(watcher);
            Assert.DoesNotContain(1, ids);
            Assert.Contains(2, ids);
        }
    }
}

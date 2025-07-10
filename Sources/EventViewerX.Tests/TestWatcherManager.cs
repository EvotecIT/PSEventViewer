using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.Concurrent;
using Xunit;

namespace EventViewerX.Tests {
    public class TestWatcherManager {
        [Fact]
        public void StartWatcherReturnsExistingInstance() {
            var first = WatcherManager.StartWatcher(
                "unit", Environment.MachineName, "Application", new List<int>(), new List<NamedEvents>(), _ => { }, 1, false, false, 0, null);
            var second = WatcherManager.StartWatcher(
                "unit", Environment.MachineName, "Application", new List<int>(), new List<NamedEvents>(), _ => { }, 1, false, false, 0, null);
            Assert.Same(first, second);
            WatcherManager.StopAll();
        }

        [Fact]
        public void StartWatcherThrowsWhenDuplicatesExist() {
            var field = typeof(WatcherManager).GetField("_watchers", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(field);
            var dict = (ConcurrentDictionary<Guid, WatcherInfo>)field!.GetValue(null)!;
            var watcher1 = new WatcherInfo("dup", Environment.MachineName, "Application", new List<int>(), new List<NamedEvents>(), _ => { }, 1, false, false, 0, null);
            var watcher2 = new WatcherInfo("dup", Environment.MachineName, "Application", new List<int>(), new List<NamedEvents>(), _ => { }, 1, false, false, 0, null);
            dict.TryAdd(Guid.NewGuid(), watcher1);
            dict.TryAdd(Guid.NewGuid(), watcher2);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                WatcherManager.StartWatcher("dup", Environment.MachineName, "Application", new List<int>(), new List<NamedEvents>(), _ => { }, 1, false, false, 0, null));
            Assert.Contains("Multiple watchers", ex.Message);
            WatcherManager.StopAll();
        }
    }
}

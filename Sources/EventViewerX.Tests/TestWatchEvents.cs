using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace EventViewerX.Tests {
    public class TestWatchEvents {
        private static HashSet<int> GetIds(WatchEvents watcher) {
            FieldInfo? field = typeof(WatchEvents).GetField("_watchEventIds", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return (HashSet<int>)field!.GetValue(watcher)!;
        }

        [Fact]
        public void DisposeClearsWatchEventIds() {
            using var watcher = new WatchEvents();
            watcher.Watch(Environment.MachineName, "Application", new List<int> { 1 });
            watcher.Dispose();
            Assert.Empty(GetIds(watcher));
        }

        [Fact]
        public void SubsequentWatchesUseNewIdsOnly() {
            using var watcher = new WatchEvents();
            watcher.Watch(Environment.MachineName, "Application", new List<int> { 1 });
            watcher.Watch(Environment.MachineName, "Application", new List<int> { 2 });
            HashSet<int> ids = GetIds(watcher);
            Assert.DoesNotContain(1, ids);
            Assert.Contains(2, ids);
        }

        [Fact]
        public void StagingAddsEvent350WithoutMutatingCallerList() {
            using var watcher = new WatchEvents();
            var ids = new List<int> { 1 };
            watcher.Watch(Environment.MachineName, "Application", ids, null, default, true, "tester");

            Assert.DoesNotContain(350, ids);
            Assert.Contains(350, GetIds(watcher));
            Assert.Equal("tester", watcher.StagingEnabledBy);
        }

        [Fact]
        public void ResetGlobalEventCountIsExplicit() {
            WatchEvents.ResetGlobalEventCount();
            Assert.Equal(0, WatchEvents.NumberOfEventsFound);
        }

        [Fact]
        public void WatchRejectsEmptyEventFilter() {
            using var watcher = new WatchEvents();
            Assert.Throws<ArgumentException>(() => watcher.Watch(null, "Application", new List<int>()));
        }

        [Fact]
        public void WatchRejectsAnAlreadyCancelledSubscription() {
            using var watcher = new WatchEvents();
            using var cancellation = new System.Threading.CancellationTokenSource();
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() => watcher.Watch(
                null,
                "Application",
                new List<int> { 1 },
                cancellationToken: cancellation.Token));
            Assert.Empty(GetIds(watcher));
        }
    }
}

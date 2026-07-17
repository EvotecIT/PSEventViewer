using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace EventViewerX.Tests {
    [Collection("WatcherManager")]
    public class TestWatcherTimeout {
        [Fact]
        public void WatcherStopsAfterTimeout() {
            if (!OperatingSystem.IsWindows()) return;
            var watcher = WatcherManager.StartWatcher(
                "timeoutTest",
                Environment.MachineName,
                "Application",
                new List<int> { 1 },
                new List<NamedEvents>(),
                _ => { },
                false,
                false,
                0,
                TimeSpan.FromMilliseconds(100)
            );

            Assert.True(
                SpinWait.SpinUntil(() => watcher.EndTime.HasValue, 5000),
                "Watcher did not stop before timeout."
            );
            WatcherManager.StopAll();
        }
    }
}

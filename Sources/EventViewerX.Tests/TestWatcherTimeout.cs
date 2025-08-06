using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace EventViewerX.Tests {
    public class TestWatcherTimeout {
        [Fact]
        public void WatcherStopsAfterTimeout() {
            if (!OperatingSystem.IsWindows()) return;
            var watcher = WatcherManager.StartWatcher(
                "timeoutTest",
                Environment.MachineName,
                "Application",
                new List<int>(),
                new List<NamedEvents>(),
                _ => { },
                1,
                false,
                false,
                0,
                TimeSpan.FromMilliseconds(100)
            );

            Assert.NotNull(watcher.TimeoutTask);
            watcher.TimeoutTask!.Wait(TimeSpan.FromSeconds(5));
            SpinWait.SpinUntil(() => watcher.EndTime.HasValue, TimeSpan.FromSeconds(5));
            Assert.NotNull(watcher.EndTime);
            WatcherManager.StopAll();
        }
    }
}

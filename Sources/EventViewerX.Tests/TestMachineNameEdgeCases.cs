using System;
using System.Collections.Generic;
using Xunit;

namespace EventViewerX.Tests {
    public class TestMachineNameEdgeCases {
        [Fact]
        public void QueryLogHandlesEmptyMachineName() {
            if (!OperatingSystem.IsWindows()) return;
            using var enumerator = SearchEvents.QueryLog(KnownLog.Setup, null, string.Empty).GetEnumerator();
            if (enumerator.MoveNext()) {
                Assert.NotNull(enumerator.Current);
            }
        }

        [Fact]
        public void WatchHandlesNullMachineName() {
            if (!OperatingSystem.IsWindows()) return;
            var watcher = new WatchEvents();
            watcher.Watch(null, "Application", new List<int> { 1 });
            watcher.Dispose();
        }
    }
}

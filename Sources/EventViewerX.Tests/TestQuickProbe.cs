using System;
using System.Threading;
using Xunit;

namespace EventViewerX.Tests {
    public class TestQuickProbe {
        [Fact]
        public void NegativeCacheExpiresAndIsReevaluated() {
            if (!OperatingSystem.IsWindows()) return;

            const string host = "203.0.113.1"; // TEST-NET non-routable
            var originalTtl = Settings.NegativeCacheTtlSeconds;
            var originalPing = Settings.PingTimeoutMs;
            var originalRpcTimeout = Settings.RpcProbeTimeoutMs;
            var originalSessionTimeout = Settings.SessionTimeoutMs;

            try {
                Settings.NegativeCacheTtlSeconds = 1;
                Settings.PingTimeoutMs = 200;
                Settings.RpcProbeTimeoutMs = 200;
                Settings.SessionTimeoutMs = 600;
                SearchEvents.ClearAllHostCache();

                var first = SearchEvents.ProbeLatestEvent("Application", machineName: host, timeout: TimeSpan.FromMilliseconds(500), maxEventsToScan: 2);
                Assert.Equal(SearchEvents.QuickProbeStatus.Error, first.Status);

                var cached = SearchEvents.ProbeLatestEvent("Application", machineName: host, timeout: TimeSpan.FromMilliseconds(300), maxEventsToScan: 2);
                Assert.Equal(SearchEvents.QuickProbeStatus.Error, cached.Status);
                Assert.Equal("Cached unreachable", cached.Message);

                Thread.Sleep(1200);

                var afterTtl = SearchEvents.ProbeLatestEvent("Application", machineName: host, timeout: TimeSpan.FromMilliseconds(500), maxEventsToScan: 2);
                Assert.Equal(SearchEvents.QuickProbeStatus.Error, afterTtl.Status);
                Assert.NotEqual("Cached unreachable", afterTtl.Message);
            }
            finally {
                Settings.NegativeCacheTtlSeconds = originalTtl;
                Settings.PingTimeoutMs = originalPing;
                Settings.RpcProbeTimeoutMs = originalRpcTimeout;
                Settings.SessionTimeoutMs = originalSessionTimeout;
                SearchEvents.ClearAllHostCache();
            }
        }

        [Fact]
        public void RpcProbeUsesConfigurablePort() {
            if (!OperatingSystem.IsWindows()) return;

            var originalPort = Settings.RpcProbePort;
            var originalRpcTimeout = Settings.RpcProbeTimeoutMs;
            var originalPing = Settings.PingTimeoutMs;

            try {
                Settings.RpcProbePort = 1; // closed port should fail fast
                Settings.RpcProbeTimeoutMs = 200;
                Settings.PingTimeoutMs = 200;
                SearchEvents.ClearAllHostCache();

                var result = SearchEvents.ProbeLatestEvent("Application", machineName: "127.0.0.1", timeout: TimeSpan.FromMilliseconds(500), maxEventsToScan: 2);

                Assert.Equal(SearchEvents.QuickProbeStatus.Error, result.Status);
                Assert.Equal("RPC probe failed", result.Message);
            }
            finally {
                Settings.RpcProbePort = originalPort;
                Settings.RpcProbeTimeoutMs = originalRpcTimeout;
                Settings.PingTimeoutMs = originalPing;
                SearchEvents.ClearAllHostCache();
            }
        }
    }
}

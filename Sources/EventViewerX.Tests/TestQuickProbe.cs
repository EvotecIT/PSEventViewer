using System;
using System.Threading;
using Xunit;

namespace EventViewerX.Tests {
    public class TestQuickProbe {
        [Fact]
        public void SessionCreationPreservesSubSecondProbeBudget() {
            if (!OperatingSystem.IsWindows()) return;

            using EventLogSessionOpenResult result = SearchEvents.CreateSessionResult(
                null,
                "QuickProbe",
                "Application",
                timeoutMs: 250);

            Assert.True(result.Success);
            Assert.Equal(250, result.TimeoutMs);
        }

        [Fact]
        public void BlankMachineNameReportsTheResolvedLocalMachine() {
            if (!OperatingSystem.IsWindows()) return;

            var result = SearchEvents.ProbeLatestEvent(
                "Definitely-Missing-EVX-Log",
                machineName: " ",
                timeout: TimeSpan.FromSeconds(2),
                maxEventsToScan: 1);

            Assert.False(string.IsNullOrWhiteSpace(result.Machine));
            Assert.Equal(SearchEvents.QuickProbeStatus.LogNotFound, result.Status);
        }

        [Fact]
        public void NegativeCacheExpiresAndIsReevaluated() {
            if (!OperatingSystem.IsWindows()) return;

            const string host = "203.0.113.1"; // TEST-NET non-routable
            var originalTtl = Settings.NegativeCacheTtlSeconds;
            var originalRpcTimeout = Settings.RpcProbeTimeoutMs;
            var originalSessionTimeout = Settings.SessionTimeoutMs;

            try {
                Settings.NegativeCacheTtlSeconds = 1;
                Settings.RpcProbeTimeoutMs = 200;
                Settings.SessionTimeoutMs = 600;
                SearchEvents.ClearAllHostCache();

                var first = SearchEvents.ProbeLatestEvent("Application", machineName: host, timeout: TimeSpan.FromMilliseconds(500), maxEventsToScan: 2);
                Assert.Equal(SearchEvents.QuickProbeStatus.HostUnavailable, first.Status);

                var cached = SearchEvents.ProbeLatestEvent("Application", machineName: host, timeout: TimeSpan.FromMilliseconds(300), maxEventsToScan: 2);
                Assert.Equal(SearchEvents.QuickProbeStatus.HostUnavailable, cached.Status);
                Assert.Contains("cached as unreachable", cached.Message, StringComparison.OrdinalIgnoreCase);

                Thread.Sleep(1200);

                var afterTtl = SearchEvents.ProbeLatestEvent("Application", machineName: host, timeout: TimeSpan.FromMilliseconds(500), maxEventsToScan: 2);
                Assert.Equal(SearchEvents.QuickProbeStatus.HostUnavailable, afterTtl.Status);
                Assert.DoesNotContain("cached as unreachable", afterTtl.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }
            finally {
                Settings.NegativeCacheTtlSeconds = originalTtl;
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

            try {
                Settings.RpcProbePort = 1; // closed port should fail fast
                Settings.RpcProbeTimeoutMs = 200;
                SearchEvents.ClearAllHostCache();

                var result = SearchEvents.ProbeLatestEvent("Application", machineName: "203.0.113.1", timeout: TimeSpan.FromMilliseconds(500), maxEventsToScan: 2);

                Assert.Equal(SearchEvents.QuickProbeStatus.HostUnavailable, result.Status);
                Assert.Contains("RPC preflight", result.Message, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("port 1", result.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally {
                Settings.RpcProbePort = originalPort;
                Settings.RpcProbeTimeoutMs = originalRpcTimeout;
                SearchEvents.ClearAllHostCache();
            }
        }
    }
}

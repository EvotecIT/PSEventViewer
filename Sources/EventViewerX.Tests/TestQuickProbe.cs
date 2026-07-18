using System;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace EventViewerX.Tests {
    public class TestQuickProbe {
        [Theory]
        [InlineData(EventLogSessionOpenStatus.NegativeCache)]
        [InlineData(EventLogSessionOpenStatus.RpcUnavailable)]
        [InlineData(EventLogSessionOpenStatus.EventLogSessionUnavailable)]
        public void RemoteSessionAvailabilityFailuresMapToHostUnavailable(EventLogSessionOpenStatus status) {
            Assert.Equal(SearchEvents.QuickProbeStatus.HostUnavailable, SearchEvents.MapSessionProbeStatus(status));
        }

        [Fact]
        public void LocalSessionCreationReturnsBeforeTheStalledFactoryCompletes() {
            if (!OperatingSystem.IsWindows()) return;

            var stopwatch = Stopwatch.StartNew();
            using EventLogSessionOpenResult result = SearchEvents.CreateSessionResult(
                null,
                "QuickProbe",
                "Application",
                timeoutMs: 100,
                localSessionFactory: () => {
                    Thread.Sleep(2000);
                    return new System.Diagnostics.Eventing.Reader.EventLogSession();
                });

            Assert.False(result.Success);
            Assert.Equal(EventLogSessionOpenStatus.Timeout, result.Status);
            Assert.Equal(100, result.TimeoutMs);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(1500), $"Elapsed {stopwatch.Elapsed.TotalMilliseconds:F0} ms.");
        }

        [Fact]
        public void RemoteSessionCreationClassifiesAStalledOpenAsTimeout() {
            if (!OperatingSystem.IsWindows()) return;

            const string host = "eventviewerx-stalled-session.invalid";
            SearchEvents.ClearHostCache(host);
            var stopwatch = Stopwatch.StartNew();
            try {
                using EventLogSessionOpenResult result = SearchEvents.CreateSessionResult(
                    host,
                    "QuickProbe",
                    "Application",
                    timeoutMs: 100,
                    rpcProbeOverride: static (_, _) => true,
                    remoteSessionFactory: static _ => {
                        Thread.Sleep(2000);
                        return new System.Diagnostics.Eventing.Reader.EventLogSession();
                    });

                Assert.False(result.Success);
                Assert.Equal(EventLogSessionOpenStatus.Timeout, result.Status);
                Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(1500), $"Elapsed {stopwatch.Elapsed.TotalMilliseconds:F0} ms.");
            } finally {
                SearchEvents.ClearHostCache(host);
            }
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
        public void ProbeLatestEvent_DoesNotWriteBoundaryDiagnostics() {
            if (!OperatingSystem.IsWindows()) return;

            const string host = "[";
            InternalLogger previous = Settings._logger;
            var logger = new InternalLogger();
            var warnings = new List<string>();
            var verboseMessages = new List<string>();
            logger.OnWarningMessage += (_, args) => warnings.Add(args.FullMessage);
            logger.OnVerboseMessage += (_, args) => verboseMessages.Add(args.FullMessage);
            Settings._logger = logger;
            SearchEvents.ClearHostCache(host);
            try {
                SearchEvents.QuickProbeResult result = SearchEvents.ProbeLatestEvent(
                    "Application",
                    machineName: host,
                    timeout: TimeSpan.FromMilliseconds(100),
                    maxEventsToScan: 1);

                Assert.Equal(SearchEvents.QuickProbeStatus.HostUnavailable, result.Status);
                Assert.Empty(warnings);
                Assert.Empty(verboseMessages);
            } finally {
                SearchEvents.ClearHostCache(host);
                Settings._logger = previous;
            }
        }

        [Fact]
        public void NegativeCacheExpiresAndIsReevaluated() {
            if (!OperatingSystem.IsWindows()) return;

            const string host = "203.0.113.77"; // Dedicated TEST-NET address avoids cross-class cache interference.
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

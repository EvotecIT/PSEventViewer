using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Xunit;

namespace EventViewerX.Tests {
    public class TestPowerShellScripts {
        [Fact]
        public void ExtractDataLogsWarning() {
            var method = typeof(SearchEvents).GetMethod(
                "ExtractData",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(EventRecord), typeof(string) },
                null);
            Assert.NotNull(method);
            string? message = null;
            EventHandler<LogEventArgs> handler = (_, e) => message = e.FullMessage;
            Settings._logger.OnWarningMessage += handler;
            try {
                var result = method!.Invoke(null, new object?[] { null, "Test" });
                Assert.Null(result);
                Assert.NotNull(message);
            } finally {
                Settings._logger.OnWarningMessage -= handler;
            }
        }

        [Fact]
        public void GetAllDataLogsWarning() {
            var method = typeof(SearchEvents).GetMethod(
                "GetAllData",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(EventRecord) },
                null);
            Assert.NotNull(method);
            string? message = null;
            EventHandler<LogEventArgs> handler = (_, e) => message = e.FullMessage;
            Settings._logger.OnWarningMessage += handler;
            try {
                var result = method!.Invoke(null, new object?[] { null });
                Assert.NotNull(result);
                Assert.Empty((Dictionary<string, string?>)result!);
                Assert.NotNull(message);
            } finally {
                Settings._logger.OnWarningMessage -= handler;
            }
        }

        [Fact]
        public void ExtractDataFromElement() {
            const string xml = "<Event><EventData><Data Name='Field'>Value</Data></EventData></Event>";
            var element = XElement.Parse(xml);
            var method = typeof(SearchEvents).GetMethod(
                "ExtractData",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(XElement), typeof(string) },
                null);
            Assert.NotNull(method);
            var result = method!.Invoke(null, new object[] { element, "Field" });
            Assert.Equal("Value", result);
        }

        [Fact]
        public void GetAllDataFromElement() {
            const string xml = "<Event><EventData><Data Name='A'>1</Data><Data Name='B'>2</Data></EventData></Event>";
            var element = XElement.Parse(xml);
            var method = typeof(SearchEvents).GetMethod(
                "GetAllData",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(XElement) },
                null);
            Assert.NotNull(method);
            var result = (Dictionary<string, string?>)method!.Invoke(null, new object[] { element })!;
            Assert.Equal("1", result["A"]);
            Assert.Equal("2", result["B"]);
        }

        [Fact]
        public void FragmentCacheReleasesACompleteScriptImmediately() {
            var cache = new PowerShellScriptFragmentCache(maxPendingScripts: 4, maxCachedEvents: 8);

            bool firstComplete = cache.TryAdd(
                "script-1",
                messageNumber: 2,
                messageTotal: 2,
                scriptText: "world",
                CreateEventObject(),
                out PowerShellScriptAssembly? first);
            bool secondComplete = cache.TryAdd(
                "script-1",
                messageNumber: 1,
                messageTotal: 2,
                scriptText: "hello ",
                CreateEventObject(),
                out PowerShellScriptAssembly? completed);

            Assert.False(firstComplete);
            Assert.Null(first);
            Assert.True(secondComplete);
            Assert.NotNull(completed);
            Assert.Equal("hello ", completed!.Parts[1]);
            Assert.Equal("world", completed.Parts[2]);
            Assert.Equal(2, completed.Events.Count);
            Assert.True(completed.IsComplete);
            Assert.Equal(0, cache.PendingScriptCount);
            Assert.Equal(0, cache.CachedEventCount);
        }

        [Fact]
        public void FragmentCacheEvictsTheOldestIncompleteScriptAtItsBounds() {
            var cache = new PowerShellScriptFragmentCache(maxPendingScripts: 2, maxCachedEvents: 2);

            cache.TryAdd("script-1", 1, 2, "one", CreateEventObject(), out _);
            cache.TryAdd("script-2", 1, 2, "two", CreateEventObject(), out _);
            cache.TryAdd("script-3", 1, 2, "three", CreateEventObject(), out _);

            Assert.False(cache.Contains("script-1"));
            Assert.True(cache.Contains("script-2"));
            Assert.True(cache.Contains("script-3"));
            Assert.Equal(2, cache.PendingScriptCount);
            Assert.Equal(2, cache.CachedEventCount);
            Assert.Equal(1, cache.EvictedScriptCount);
            Assert.Equal(1, cache.EvictedEventCount);
        }

        [Fact]
        public void FragmentCacheRejectsUntrustedOversizedPartDeclarations() {
            var cache = new PowerShellScriptFragmentCache(maxPendingScripts: 2, maxCachedEvents: 2);

            Assert.Throws<ArgumentOutOfRangeException>(() => cache.TryAdd(
                "script-1",
                messageNumber: 1,
                messageTotal: int.MaxValue,
                scriptText: "content",
                CreateEventObject(),
                out _));
        }

        [Fact]
        public void ScriptReconstructionUsesAvailablePartKeysInsteadOfTheDeclaredRange() {
            var assembly = new PowerShellScriptAssembly(
                "script-1",
                metaRecord: null,
                events: new[] { CreateEventObject() },
                parts: new Dictionary<int, string> {
                    [1] = "first",
                    [SearchEvents.MaximumPowerShellScriptPartCount] = "last"
                },
                expectedParts: int.MaxValue,
                isComplete: false);
            MethodInfo method = typeof(SearchEvents).GetMethod(
                "TryBuildRestoredPowerShellScript",
                BindingFlags.NonPublic | BindingFlags.Static)!;
            object?[] arguments = { assembly, false, new[] { "not-present" }, null };

            bool matched = Assert.IsType<bool>(method.Invoke(null, arguments));

            Assert.False(matched);
        }

        [Fact]
        public void QueryExecutionInfoMakesBoundedOrIncompleteResultsMachineReadable() {
            var info = new PowerShellScriptQueryExecutionInfo();
            info.Reset("AD1", null, maxResults: 10, maxEventsScanned: 100);
            info.EventsScanned = 100;
            info.ScanLimitReached = true;

            Assert.True(info.MayBeIncomplete);
            Assert.Equal("AD1", info.MachineName);
            Assert.Equal(100, info.EventsScanned);
        }

        [Fact]
        public void GetPowerShellScriptsRejectsInvalidBoundsBeforeOpeningTheLog() {
            Assert.Throws<ArgumentOutOfRangeException>(() => SearchEvents.GetPowerShellScripts(
                PowerShellEdition.WindowsPowerShell,
                maxScripts: -1).ToList());
            Assert.Throws<ArgumentOutOfRangeException>(() => SearchEvents.GetPowerShellScripts(
                PowerShellEdition.WindowsPowerShell,
                maxPendingScripts: 0).ToList());
        }

        [Fact]
        public void GetPowerShellScriptsHonorsPreCancelledRequestsBeforeOpeningTheLog() {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() => SearchEvents.GetPowerShellScripts(
                PowerShellEdition.WindowsPowerShell,
                null,
                null,
                null,
                null,
                false,
                null,
                0,
                0,
                SearchEvents.DefaultPowerShellScriptPendingLimit,
                SearchEvents.DefaultPowerShellScriptEventCacheLimit,
                cancellation.Token).ToList());
            Assert.Throws<OperationCanceledException>(() => SearchEvents.GetPowerShellScriptExecution(
                PowerShellEdition.WindowsPowerShell,
                null,
                null,
                null,
                null,
                0,
                0,
                cancellation.Token).ToList());
        }

        private static EventObject CreateEventObject() {
            return (EventObject)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(EventObject));
        }
    }
}

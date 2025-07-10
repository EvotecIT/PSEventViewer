using System.Reflection;
using System.Collections.Generic;
using Xunit;

namespace EventViewerX.Tests {
    public class TestPowerShellScripts {
        [Fact]
        public void ExtractDataLogsWarning() {
            var method = typeof(SearchEvents).GetMethod("ExtractData", BindingFlags.NonPublic | BindingFlags.Static);
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
            var method = typeof(SearchEvents).GetMethod("GetAllData", BindingFlags.NonPublic | BindingFlags.Static);
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
    }
}

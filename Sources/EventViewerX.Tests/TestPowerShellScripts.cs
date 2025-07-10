using System.Reflection;
using System.Collections.Generic;
using System.Xml.Linq;
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
    }
}

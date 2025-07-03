using System;
using System.IO;
using System.Linq;
using Xunit;

namespace EventViewerX.Tests {
    public class TestQueryLogFile {
        [Fact]
        public void QueryLogFileSanitizesPath() {
            if (!OperatingSystem.IsWindows()) return;
            string path = Path.Combine("..", "..", "..", "..", "..", "Tests", "Logs", "Active Directory Web Services.evtx");
            string quotedPath = $"\"{path}\"";
            var events = SearchEvents.QueryLogFile(quotedPath).ToList();
            Assert.NotEmpty(events);
        }
    }
}

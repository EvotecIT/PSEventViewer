using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace EventViewerX.Tests
{

    /// <summary>
    /// Unit tests for WinEventInformation retrieval.
    /// </summary>
    public class TestEventInformation {
        [Fact]
        public void QueryLocalLogInformation() {
            if (!OperatingSystem.IsWindows()) return;
            var result = SearchEvents.GetWinEventInformation(["Application"], [Environment.MachineName], null, 1).ToList();
            Assert.True(result.Count > 0);
            Assert.Equal(Environment.MachineName, result[0].Source);
            Assert.Equal("Application", result[0].LogName);
        }

        [Fact]
        public void QueryFileInformation() {
            if (!OperatingSystem.IsWindows()) return;
            var path = Path.Combine("..", "..", "..", "..", "..", "Tests", "Logs", "Active Directory Web Services.evtx");
            var result = SearchEvents.GetWinEventInformation(null, null, new List<string> { path }, 1).ToList();
            Assert.Single(result);
            Assert.Equal("File", result[0].Source);
            Assert.Equal("N/A", result[0].LogName);
        }
    }
}
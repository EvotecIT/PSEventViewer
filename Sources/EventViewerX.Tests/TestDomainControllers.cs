using System;
using Xunit;

namespace EventViewerX.Tests {
    public class TestDomainControllers {
        [Fact]
        public void GetDomainControllersFallsBackToEnvironmentVariable() {
            if (!OperatingSystem.IsWindows()) return;
            var original = Environment.GetEnvironmentVariable("LOGONSERVER");
            try {
                Environment.SetEnvironmentVariable("LOGONSERVER", "\\FAKEDC");
                var result = SearchEvents.GetDomainControllers();
                Assert.Contains("FAKEDC", result);
            } finally {
                Environment.SetEnvironmentVariable("LOGONSERVER", original);
            }
        }
    }
}

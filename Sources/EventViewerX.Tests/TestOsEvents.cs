using System.Collections.Generic;
using Xunit;

namespace EventViewerX.Tests
{

    /// <summary>
    /// Unit tests for OS event queries.
    /// </summary>
    public class TestOsEvents {
        [Fact]
        public void EventInfoContainsOsEvents() {
            var info = EventObjectSlim.GetEventInfoForNamedEvents(new List<NamedEvents> {
                NamedEvents.OSStartup,
                NamedEvents.OSShutdown,
                NamedEvents.OSUncleanShutdown,
                NamedEvents.OSStartupSecurity,
                NamedEvents.OSCrashOnAuditFailRecovery
            });

            Assert.Contains(12, info["System"]);
            Assert.Contains(13, info["System"]);
            Assert.Contains(41, info["System"]);
            Assert.Contains(4608, info["Security"]);
            Assert.Contains(4621, info["Security"]);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace EventViewerX.Tests {
    public class TestStreaming {
        [Fact]
        public async Task QueryLogsParallelStreamsFirstEvent() {
            if (!OperatingSystem.IsWindows()) return;
            await foreach (var _ in SearchEvents.QueryLogsParallel("System", maxEvents: 1, machineNames: new List<string> { Environment.MachineName })) {
                // Successfully retrieved first event, so streaming works
                return;
            }
            Assert.Fail("No events were returned from QueryLogsParallel.");
        }

        [Fact]
        public async Task NamedEventsStreamFirstEvent() {
            if (!OperatingSystem.IsWindows()) return;
            await foreach (var _ in SearchEvents.FindEventsByNamedEvents([NamedEvents.OSStartupShutdownCrash], new List<string> { Environment.MachineName }, maxEvents: 1)) {
                return;
            }
            Assert.Fail("No events were returned from FindEventsByNamedEvents.");
        }
    }
}

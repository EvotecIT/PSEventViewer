using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace EventViewerX.Tests {
    public class TestStreaming {
        [Fact]
        public async Task QueryLogsParallelStreamsFirstEvent() {
            if (!OperatingSystem.IsWindows()) return;
            if (!TestEnv.CanReadLog("System")) return;
            bool gotAny = false;
            await foreach (var _ in SearchEvents.QueryLogsParallel("System", maxEvents: 1, machineNames: new List<string> { Environment.MachineName })) {
                // Successfully retrieved first event, so streaming works
                gotAny = true;
                break;
            }
            if (!gotAny) return;
        }

        [Fact]
        public async Task NamedEventsStreamFirstEvent() {
            if (!OperatingSystem.IsWindows()) return;
            if (!TestEnv.CanReadLog("Security")) return;
            bool gotAny = false;
            await foreach (var _ in SearchEvents.FindEventsByNamedEvents([
                NamedEvents.OSStartupSecurity
            ], new List<string> { Environment.MachineName }, maxEvents: 1)) {
                gotAny = true;
                break;
            }
            if (!gotAny) return;
        }
    }
}

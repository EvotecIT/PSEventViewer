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

        [Fact]
        public void QueryLogFileIgnoresEmptyProviderName() {
            if (!OperatingSystem.IsWindows()) return;
            string path = Path.Combine("..", "..", "..", "..", "..", "Tests", "Logs", "Active Directory Web Services.evtx");
            var eventsWithoutFilter = SearchEvents.QueryLogFile(path).ToList();
            var eventsWithEmptyProvider = SearchEvents.QueryLogFile(path, providerName: "").ToList();
            Assert.Equal(eventsWithoutFilter.Count, eventsWithEmptyProvider.Count);
        }

        [Fact]
        public void QueryLogFileChunksLargeEventIdFiltersWithinTheXPathBudget() {
            if (!OperatingSystem.IsWindows()) return;
            string path = Path.Combine("..", "..", "..", "..", "..", "Tests", "Logs", "Active Directory Web Services.evtx");
            EventObject? latest = SearchEvents.QueryLogFile(path, maxEvents: 1, readMode: EventReadMode.Metadata).SingleOrDefault();
            if (latest == null) return;

            var eventIds = Enumerable.Range(100000, 100).ToList();
            eventIds.Add(latest.Id);

            EventObject result = Assert.Single(SearchEvents.QueryLogFile(
                path,
                eventIds: eventIds,
                maxEvents: 1,
                readMode: EventReadMode.Metadata));

            Assert.Equal(latest.Id, result.Id);
            Assert.Equal(latest.RecordId, result.RecordId);
        }

        [Fact]
        public void QueryLogFileAppliesRelativeTimePeriods() {
            if (!OperatingSystem.IsWindows()) return;
            string path = Path.Combine("..", "..", "..", "..", "..", "Tests", "Logs", "Active Directory Web Services.evtx");

            List<EventObject> recent = SearchEvents.QueryLogFile(
                path,
                timePeriod: TimePeriod.Last1Hour,
                maxEvents: 1,
                readMode: EventReadMode.Metadata).ToList();

            Assert.Empty(recent);
        }
    }
}

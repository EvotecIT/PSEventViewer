using System;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using Xunit;

namespace EventViewerX.Tests {
    public class TestQueryLogFile {
        [Fact]
        public void MetadataProjectionMatchesDirectEventRecordSnapshot() {
            if (!OperatingSystem.IsWindows()) return;
            string relativePath = Path.Combine("..", "..", "..", "..", "..", "Tests", "Logs", "NamedFilterExamples.evtx");
            string path = Path.GetFullPath(relativePath);
            var query = new EventLogQuery(path, PathType.FilePath, "*") {
                ReverseDirection = false,
                TolerateQueryErrors = false
            };
            using var reader = new EventLogReader(query);
            EventRecord record = Assert.IsAssignableFrom<EventRecord>(reader.ReadEvent());
            var expected = new EventObject(record, path, EventReadMode.Metadata);

            EventObject actual = Assert.Single(SearchEvents.QueryLogFile(
                path,
                maxEvents: 1,
                oldest: true,
                readMode: EventReadMode.Metadata));

            Assert.Equal(expected.TimeCreated, actual.TimeCreated);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.RecordId, actual.RecordId);
            Assert.Equal(expected.LogName, actual.LogName);
            Assert.Equal(expected.ContainerLog, actual.ContainerLog);
            Assert.Equal(expected.MachineName, actual.MachineName);
            Assert.Equal(expected.ProviderName, actual.ProviderName);
            Assert.Equal(expected.Qualifiers, actual.Qualifiers);
            Assert.Equal(expected.Opcode, actual.Opcode);
            Assert.Equal(expected.ProviderId, actual.ProviderId);
            Assert.Equal(expected.RelatedActivityId, actual.RelatedActivityId);
            Assert.Equal(expected.ActivityId, actual.ActivityId);
            Assert.Equal(expected.UserId?.Value, actual.UserId?.Value);
            Assert.Null(actual.Bookmark);
            Assert.Equal(expected.Keywords, actual.Keywords);
            Assert.Equal(expected.Level, actual.Level);
            Assert.Equal(expected.Version, actual.Version);
            Assert.Equal(expected.Task, actual.Task);
            Assert.Equal(expected.ProcessId, actual.ProcessId);
            Assert.Equal(expected.ThreadId, actual.ThreadId);
            Assert.Equal(expected.LevelDisplayName, actual.LevelDisplayName);
            Assert.Equal(expected.GatheredFrom, actual.GatheredFrom);
            Assert.Equal(expected.GatheredLogName, actual.GatheredLogName);
        }

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
        public void QueryLogFileBoundsPrefetchAcrossXPathChunks() {
            if (!OperatingSystem.IsWindows()) return;
            string path = Path.Combine("..", "..", "..", "..", "..", "Tests", "Logs", "Active Directory Web Services.evtx");
            var eventIds = Enumerable.Range(100000, 463).ToList();
            eventIds.Insert(0, 1200);
            eventIds.Insert(100, 1100);
            int observed = 0;
            var observedIds = new HashSet<int>();

            List<EventObject> events = SearchEvents.QueryLogFile(
                path,
                eventIds: eventIds,
                maxEvents: 2,
                readMode: EventReadMode.Metadata,
                candidateObserver: eventObject => {
                    observed++;
                    observedIds.Add(eventObject.Id);
                }).ToList();

            Assert.Equal(2, events.Count);
            Assert.InRange(observed, events.Count, 5);
            Assert.Contains(1200, observedIds);
            Assert.Contains(1100, observedIds);
        }

        [Fact]
        public void QueryLogFileCombinesEventAndRecordIdFilters() {
            if (!OperatingSystem.IsWindows()) return;
            string path = Path.Combine("..", "..", "..", "..", "..", "Tests", "Logs", "Active Directory Web Services.evtx");
            EventObject? latest = SearchEvents.QueryLogFile(path, maxEvents: 1, readMode: EventReadMode.Metadata).SingleOrDefault();
            if (latest?.RecordId is not long recordId) return;

            EventObject matching = Assert.Single(SearchEvents.QueryLogFile(
                path,
                eventIds: new() { latest.Id },
                eventRecordId: new() { recordId },
                readMode: EventReadMode.Metadata));
            List<EventObject> mismatched = SearchEvents.QueryLogFile(
                path,
                eventIds: new() { latest.Id == int.MaxValue ? latest.Id - 1 : latest.Id + 1 },
                eventRecordId: new() { recordId },
                readMode: EventReadMode.Metadata).ToList();

            Assert.Equal(recordId, matching.RecordId);
            Assert.Empty(mismatched);
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

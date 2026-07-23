using System;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
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

            AssertMetadataEqual(expected, actual);
            Assert.Null(actual.Bookmark);
        }

        [Fact]
        public void MetadataProjectionMatchesEveryDirectRecordInBothDirections() {
            if (!OperatingSystem.IsWindows()) return;
            string relativePath = Path.Combine("..", "..", "..", "..", "..", "Tests", "Logs", "Active Directory Web Services.evtx");
            string path = Path.GetFullPath(relativePath);

            foreach (bool oldest in new[] { true, false }) {
                var query = new EventLogQuery(path, PathType.FilePath, "*") {
                    ReverseDirection = !oldest,
                    TolerateQueryErrors = false
                };
                using var reader = new EventLogReader(query);
                using var expectedEnumerator = ReadManagedMetadata(reader, path).GetEnumerator();
                using var actualEnumerator = SearchEvents.QueryLogFile(
                    path,
                    oldest: oldest,
                    readMode: EventReadMode.Metadata).GetEnumerator();

                int compared = 0;
                while (expectedEnumerator.MoveNext()) {
                    Assert.True(actualEnumerator.MoveNext());
                    AssertMetadataEqual(expectedEnumerator.Current, actualEnumerator.Current);
                    compared++;
                }

                Assert.False(actualEnumerator.MoveNext());
                Assert.True(compared > 1);
            }
        }

        [Fact]
        public void MessageProjectionMatchesEveryDirectRecord() {
            if (!OperatingSystem.IsWindows()) return;
            string relativePath = Path.Combine("..", "..", "..", "..", "..", "Tests", "Logs", "NamedFilterExamples.evtx");
            string path = Path.GetFullPath(relativePath);
            var query = new EventLogQuery(path, PathType.FilePath, "*") {
                ReverseDirection = false,
                TolerateQueryErrors = false
            };
            using var reader = new EventLogReader(query);
            using var expectedEnumerator = ReadManaged(reader, path, EventReadMode.Message).GetEnumerator();
            using var actualEnumerator = SearchEvents.QueryLogFile(
                path,
                oldest: true,
                readMode: EventReadMode.Message).GetEnumerator();

            int compared = 0;
            while (expectedEnumerator.MoveNext()) {
                Assert.True(actualEnumerator.MoveNext());
                EventObject expected = expectedEnumerator.Current;
                EventObject actual = actualEnumerator.Current;
                AssertMetadataEqual(expected, actual);
                Assert.Equal(expected.Message, actual.Message);
                Assert.Equal(expected.LevelDisplayName, actual.LevelDisplayName);
                Assert.Equal(expected.TaskDisplayName, actual.TaskDisplayName);
                Assert.Equal(expected.OpcodeDisplayName, actual.OpcodeDisplayName);
                Assert.Equal(expected.KeywordsDisplayNames, actual.KeywordsDisplayNames);
                Assert.Equal(expected.Bookmark == null, actual.Bookmark == null);
                Assert.NotEqual(EventMessageRenderStatus.NotRequested, actual.MessageRenderStatus);
                if (actual.MessageRenderStatus == EventMessageRenderStatus.Rendered) {
                    Assert.Equal(0, actual.MessageRenderErrorCode);
                } else {
                    Assert.NotEqual(0, actual.MessageRenderErrorCode);
                }
                compared++;
            }

            Assert.False(actualEnumerator.MoveNext());
            Assert.True(compared > 1);
        }

        [Theory]
        [InlineData(EventReadMode.StructuredData)]
        [InlineData(EventReadMode.Full)]
        public void PayloadProjectionMatchesEveryDirectRecord(EventReadMode readMode) {
            if (!OperatingSystem.IsWindows()) return;
            string relativePath = Path.Combine("..", "..", "..", "..", "..", "Tests", "Logs", "NamedFilterExamples.evtx");
            string path = Path.GetFullPath(relativePath);
            var query = new EventLogQuery(path, PathType.FilePath, "*") {
                ReverseDirection = false,
                TolerateQueryErrors = false
            };
            using var reader = new EventLogReader(query);
            using var expectedEnumerator = ReadManaged(reader, path, readMode).GetEnumerator();
            using var actualEnumerator = SearchEvents.QueryLogFile(
                path,
                oldest: true,
                readMode: readMode).GetEnumerator();

            int compared = 0;
            while (expectedEnumerator.MoveNext()) {
                Assert.True(actualEnumerator.MoveNext());
                EventObject expected = expectedEnumerator.Current;
                EventObject actual = actualEnumerator.Current;
                AssertMetadataEqual(expected, actual);
                Assert.Equal(expected.XMLData, actual.XMLData);
                Assert.Equal(expected.Data, actual.Data);
                Assert.Equal(expected.Properties.Count, actual.Properties.Count);
                for (int index = 0; index < expected.Properties.Count; index++) {
                    AssertPropertyValueEqual(
                        expected.Properties[index].Value,
                        actual.Properties[index].Value);
                }
                Assert.Equal(
                    expected.Attachments.Select(Convert.ToBase64String),
                    actual.Attachments.Select(Convert.ToBase64String));
                if (readMode == EventReadMode.Full) {
                    Assert.Equal(expected.Message, actual.Message);
                    Assert.Equal(expected.MessageData, actual.MessageData);
                }
                compared++;
            }

            Assert.False(actualEnumerator.MoveNext());
            Assert.True(compared > 1);
        }

        [Fact]
        public void PublicEngineProducesDeterministicRequestedCulture() {
            if (!OperatingSystem.IsWindows()) return;
            string relativePath = Path.Combine("..", "..", "..", "..", "..", "Tests", "Logs", "NamedFilterExamples.evtx");
            string path = Path.GetFullPath(relativePath);
            CultureInfo originalCulture = CultureInfo.CurrentUICulture;
            try {
                var query = new EventLogFileQuery(path) {
                    Oldest = true,
                    ReadMode = EventReadMode.Message,
                    MessageCulture = CultureInfo.GetCultureInfo("en-US"),
                    MaxEvents = 10
                };

                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("pl-PL");
                List<EventObject> polishHost = EventLogEngine.ReadFile(query).ToList();
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
                List<EventObject> germanHost = EventLogEngine.ReadFile(query).ToList();

                Assert.Equal(10, polishHost.Count);
                Assert.Equal(polishHost.Select(static item => item.RecordId), germanHost.Select(static item => item.RecordId));
                Assert.Equal(polishHost.Select(static item => item.Message), germanHost.Select(static item => item.Message));
                Assert.All(polishHost, static item => Assert.Equal("en-US", item.MessageCulture));
                Assert.All(polishHost, static item =>
                    Assert.NotEqual(EventMessageRenderStatus.NotRequested, item.MessageRenderStatus));
                Assert.Contains(polishHost, static item => !string.IsNullOrEmpty(item.Message));
            } finally {
                CultureInfo.CurrentUICulture = originalCulture;
            }
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

        private static IEnumerable<EventObject> ReadManagedMetadata(EventLogReader reader, string path) {
            return ReadManaged(reader, path, EventReadMode.Metadata);
        }

        private static IEnumerable<EventObject> ReadManaged(
            EventLogReader reader,
            string path,
            EventReadMode readMode) {

            while (reader.ReadEvent() is EventRecord record) {
                yield return new EventObject(record, path, readMode);
            }
        }

        private static void AssertMetadataEqual(EventObject expected, EventObject actual) {
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

        private static void AssertPropertyValueEqual(object? expected, object? actual) {
            if (expected is Array expectedArray && actual is Array actualArray) {
                Assert.Equal(
                    expectedArray.Cast<object?>().Select(static value => value?.ToString()),
                    actualArray.Cast<object?>().Select(static value => value?.ToString()));
                return;
            }

            Assert.Equal(expected?.GetType(), actual?.GetType());
            Assert.Equal(expected?.ToString(), actual?.ToString());
        }
    }
}

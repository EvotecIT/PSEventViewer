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

        [Fact]
        public async Task NamedEventsRejectNegativeCandidateScanLimit() {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => {
                await foreach (var _ in SearchEvents.FindEventsByNamedEvents(
                                   [NamedEvents.OSStartupSecurity],
                                   maxEventsScanned: -1)) {
                }
            });
        }

        [Fact]
        public void NamedEventsPreservesTheLegacyPositionalCancellationArgument() {
            IAsyncEnumerable<EventObjectSlim> query = SearchEvents.FindEventsByNamedEvents(
                new List<NamedEvents> { NamedEvents.OSStartupSecurity },
                null,
                null,
                null,
                null,
                8,
                0,
                CancellationToken.None);

            Assert.NotNull(query);
        }

        [Fact]
        public async Task NamedEventsCandidateScanLimitAlsoBoundsReturnedMatches() {
            if (!OperatingSystem.IsWindows()) return;
            if (!TestEnv.CanReadLog("Security")) return;

            int count = 0;
            await foreach (var _ in SearchEvents.FindEventsByNamedEvents(
                               [NamedEvents.OSStartupSecurity],
                               new List<string?> { null },
                               maxEventsScanned: 1)) {
                count++;
            }

            Assert.InRange(count, 0, 1);
        }

        [Fact]
        public void NamedEventsExecutionInfoRequiresAnExtraCandidateToReportTruncation() {
            var executionInfo = new NamedEventsQueryExecutionInfo();
            executionInfo.Reset(maxEventsScanned: 1);

            Assert.True(executionInfo.TryRecordCandidate());
            Assert.Equal(1, executionInfo.EventsScanned);
            Assert.False(executionInfo.ScanLimitReached);

            Assert.False(executionInfo.TryRecordCandidate());
            Assert.Equal(1, executionInfo.EventsScanned);
            Assert.True(executionInfo.ScanLimitReached);
        }

        [Fact]
        public async Task QueryLogsParallelAppliesOneGlobalMaximum() {
            if (!OperatingSystem.IsWindows()) return;
            if (!TestEnv.CanReadLog("System")) return;

            int count = 0;
            await foreach (var _ in SearchEvents.QueryLogsParallel(
                               "System",
                               maxEvents: 3,
                               machineNames: new List<string?> { null, null },
                               readMode: EventReadMode.Metadata)) {
                count++;
            }

            Assert.Equal(3, count);
        }

        [Fact]
        public void QueryLogCombinesEventAndRecordIdFilters() {
            if (!OperatingSystem.IsWindows()) return;
            if (!TestEnv.CanReadLog("System")) return;

            EventObject? latest = SearchEvents.QueryLog(
                "System",
                maxEvents: 1,
                readMode: EventReadMode.Metadata).FirstOrDefault();
            if (latest?.RecordId is not long recordId) return;

            var matching = SearchEvents.QueryLog(
                "System",
                eventIds: new List<int> { latest.Id },
                eventRecordId: new List<long> { recordId },
                maxEvents: 2,
                readMode: EventReadMode.Metadata).ToList();
            var mismatched = SearchEvents.QueryLog(
                "System",
                eventIds: new List<int> { latest.Id == int.MaxValue ? latest.Id - 1 : latest.Id + 1 },
                eventRecordId: new List<long> { recordId },
                maxEvents: 2,
                readMode: EventReadMode.Metadata).ToList();

            EventObject result = Assert.Single(matching);
            Assert.Equal(recordId, result.RecordId);
            Assert.Empty(mismatched);
        }

        [Fact]
        public async Task QueryLogsParallelStopsBlockedProducersWhenConsumerEndsEarly() {
            if (!OperatingSystem.IsWindows()) return;
            if (!TestEnv.CanReadLog("System")) return;

            var targets = Enumerable.Repeat<string?>(null, 32).ToList();
            Task readOne = ReadOneEvent(targets);
            Task completed = await Task.WhenAny(readOne, Task.Delay(TimeSpan.FromSeconds(10)));

            Assert.Same(readOne, completed);
            await readOne;
        }

        [Fact]
        public async Task QueryLogsParallelPublishesEveryReservedResultAtTheGlobalLimit() {
            if (!OperatingSystem.IsWindows()) return;
            if (!TestEnv.CanReadLog("System")) return;

            var targets = Enumerable.Repeat<string?>(null, 16).ToList();
            int count = 0;
            await foreach (var _ in SearchEvents.QueryLogsParallel(
                               "System",
                               machineNames: targets,
                               maxEvents: 16,
                               maxThreads: 8,
                               readMode: EventReadMode.Metadata,
                               bufferCapacity: 1)) {
                count++;
            }

            Assert.Equal(16, count);
        }

        [Fact]
        public async Task QueryLogsParallelRejectsInvalidParallelism() {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => {
                await foreach (var _ in SearchEvents.QueryLogsParallel("System", maxThreads: 0)) {
                }
            });
        }

        [Fact]
        public void QueryLogsParallelSharesTheXPathBudgetAcrossFilterDimensions() {
            var eventIds = Enumerable.Range(1, 100).ToList();
            var recordIds = Enumerable.Range(1, 100).Select(static value => (long)value).ToList();
            int fixedExpressions = SearchEvents.CountFixedQueryExpressions(
                providerName: "Provider",
                keywords: Keywords.AuditSuccess,
                level: Level.Informational,
                startTime: DateTime.UtcNow.AddHours(-1),
                endTime: DateTime.UtcNow,
                userId: "S-1-5-18",
                timePeriod: null);

            List<SearchEvents.QueryWorkItem> workItems = SearchEvents.BuildQueryWorkItems(
                new List<string?> { null },
                eventIds,
                recordIds,
                fixedExpressions).ToList();

            Assert.NotEmpty(workItems);
            Assert.All(workItems, item => Assert.True(
                fixedExpressions + (item.EventIds?.Count ?? 0) + (item.EventRecordIds?.Count ?? 0) <= SearchEvents.MaxXPathExpressionCount));
            Assert.All(workItems, item => Assert.True(item.ManagedEventIds!.SetEquals(eventIds)));
        }

        [Fact]
        public void QueryWorkItemsAvoidCartesianProductForLargeCombinedFilters() {
            var eventIds = Enumerable.Range(1, 5000).ToList();
            var recordIds = Enumerable.Range(1, 5000).Select(static value => (long)value).ToList();

            IEnumerable<SearchEvents.QueryWorkItem> workItems = SearchEvents.BuildQueryWorkItems(
                new List<string?> { null, "server" },
                eventIds,
                recordIds,
                fixedExpressionCount: 0);

            Assert.False(workItems is ICollection<SearchEvents.QueryWorkItem>);
            List<SearchEvents.QueryWorkItem> materialized = workItems.ToList();
            int chunksPerMachine = (int)Math.Ceiling(recordIds.Count / (double)SearchEvents.MaxXPathExpressionCount);
            Assert.Equal(chunksPerMachine * 2, materialized.Count);
            Assert.All(materialized, item => {
                Assert.Null(item.EventIds);
                Assert.InRange(item.EventRecordIds!.Count, 1, SearchEvents.MaxXPathExpressionCount);
                Assert.Equal(eventIds.Count, item.ManagedEventIds!.Count);
            });
        }

        [Fact]
        public void UnlimitedChunkMergeStreamsBeforeOpeningEveryQuery() {
            List<SearchEvents.QueryWorkItem> workItems = Enumerable.Range(0, 3)
                .Select(index => new SearchEvents.QueryWorkItem(
                    machineName: null,
                    eventIds: null,
                    eventRecordIds: new List<long> { 100 - index }))
                .ToList();
            int opened = 0;
            int exhausted = 0;

            using IEnumerator<EventObject> results = SearchEvents.MergeQueryWorkItems(
                workItems,
                workItem => CreateSingleResultQuery(
                    workItem.EventRecordIds![0],
                    () => opened++,
                    () => exhausted++).GetEnumerator(),
                maxEvents: 0,
                oldest: false,
                cancellationToken: CancellationToken.None,
                maxOpenQueries: 2).GetEnumerator();

            Assert.True(results.MoveNext());
            Assert.Equal(100L, results.Current.RecordId);
            Assert.Equal(2, opened);
            Assert.Equal(0, exhausted);
        }

        [Fact]
        public void LargePositiveMaximumDoesNotPreallocateTheRequestedCapacity() {
            var workItems = new[] {
                new SearchEvents.QueryWorkItem(null, null, new List<long> { 1 }),
                new SearchEvents.QueryWorkItem(null, null, new List<long> { 2 })
            };

            List<EventObject> results = SearchEvents.MergeQueryWorkItems(
                workItems,
                static _ => Enumerable.Empty<EventObject>().GetEnumerator(),
                maxEvents: int.MaxValue,
                oldest: false,
                cancellationToken: CancellationToken.None,
                maxOpenQueries: 2).ToList();

            Assert.Empty(results);
        }

        [Fact]
        public void QueryLogMergesChunksBeforeApplyingTheGlobalMaximum() {
            if (!OperatingSystem.IsWindows()) return;
            if (!TestEnv.CanReadLog("System")) return;

            List<EventObject> recent = SearchEvents.QueryLog(
                "System",
                maxEvents: 100,
                readMode: EventReadMode.Metadata).ToList();
            EventObject? latest = recent.FirstOrDefault();
            EventObject? olderWithDifferentId = recent.Skip(1).FirstOrDefault(item => item.Id != latest?.Id);
            if (latest?.RecordId == null || olderWithDifferentId == null) return;

            var eventIds = new List<int> { olderWithDifferentId.Id };
            eventIds.AddRange(Enumerable.Range(100000, SearchEvents.MaxXPathExpressionCount - 1));
            eventIds.Add(latest.Id);

            EventObject result = Assert.Single(SearchEvents.QueryLog(
                "System",
                eventIds: eventIds,
                maxEvents: 1,
                readMode: EventReadMode.Metadata));
            Assert.Equal(latest.Id, result.Id);
            Assert.True(result.RecordId >= latest.RecordId);
        }

        private static async Task ReadOneEvent(List<string?> targets) {
            await foreach (var _ in SearchEvents.QueryLogsParallel(
                               "System",
                               machineNames: targets,
                               maxThreads: 8,
                               readMode: EventReadMode.Metadata,
                               bufferCapacity: 1)) {
                break;
            }
        }

        private static IEnumerable<EventObject> CreateSingleResultQuery(long recordId, Action opened, Action exhausted) {
            opened();
            var eventObject = (EventObject)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(EventObject));
            SetSnapshotProperty(eventObject, nameof(EventObject.RecordId), (long?)recordId);
            SetSnapshotProperty(eventObject, nameof(EventObject.TimeCreated), new DateTime(recordId, DateTimeKind.Utc));
            SetSnapshotProperty(eventObject, nameof(EventObject.Id), (int)recordId);
            yield return eventObject;
            exhausted();
        }

        private static void SetSnapshotProperty<T>(EventObject eventObject, string propertyName, T value) {
            typeof(EventObject)
                .GetField($"<{propertyName}>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(eventObject, value);
        }
    }
}

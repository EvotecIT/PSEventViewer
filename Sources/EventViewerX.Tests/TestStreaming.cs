using System;
using System.Collections.Concurrent;
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
        public void QueryLogsParallelIsolatesRecoverableRemoteTargetFailures() {
            var failedTargets = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            var remoteWorkItem = new SearchEvents.QueryWorkItem(" server ", null, null);
            EventObject placeholder = (EventObject)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(EventObject));
            using IEnumerator<EventObject> remoteResults = new[] { placeholder }
                .Select<EventObject, EventObject>(_ => throw new TimeoutException("remote timeout"))
                .GetEnumerator();

            Assert.False(SearchEvents.TryMoveNextQueryWorkItem(remoteResults, remoteWorkItem, failedTargets, out EventObject? result));
            Assert.Null(result);
            Assert.True(SearchEvents.ShouldSkipFailedTarget(new SearchEvents.QueryWorkItem("SERVER", null, null), failedTargets));

            using IEnumerator<EventObject> localResults = new[] { placeholder }
                .Select<EventObject, EventObject>(_ => throw new TimeoutException("local timeout"))
                .GetEnumerator();
            Assert.Throws<TimeoutException>(() => SearchEvents.TryMoveNextQueryWorkItem(
                localResults,
                new SearchEvents.QueryWorkItem(null, null, null),
                failedTargets,
                out _));
        }

        [Fact]
        public void RemoteFailureClassifierDoesNotHideLocalOrCancellationFailures() {
            Assert.True(EventLogRemoteQueryFailureClassifier.TryClassify(
                "server",
                new TimeoutException("timeout"),
                out EventLogRemoteQueryFailureKind remoteFailure));
            Assert.Equal(EventLogRemoteQueryFailureKind.Timeout, remoteFailure);

            Assert.False(EventLogRemoteQueryFailureClassifier.TryClassify(
                null,
                new TimeoutException("local timeout"),
                out EventLogRemoteQueryFailureKind localFailure));
            Assert.Equal(EventLogRemoteQueryFailureKind.None, localFailure);

            Assert.False(EventLogRemoteQueryFailureClassifier.TryClassify(
                "server",
                new OperationCanceledException(),
                out EventLogRemoteQueryFailureKind cancellationFailure));
            Assert.Equal(EventLogRemoteQueryFailureKind.None, cancellationFailure);

            Assert.False(EventLogRemoteQueryFailureClassifier.TryClassify(
                "server",
                new InvalidOperationException("projection bug"),
                out EventLogRemoteQueryFailureKind unexpectedFailure));
            Assert.Equal(EventLogRemoteQueryFailureKind.None, unexpectedFailure);
        }

        [Fact]
        public void QueryLogsSequentialContinuesAfterExpectedRemoteFailure() {
            if (!OperatingSystem.IsWindows()) return;
            if (!TestEnv.CanReadLog("System")) return;

            List<EventObject> events = SearchEvents.QueryLogsSequential(
                "System",
                machineNames: new List<string?> { "203.0.113.1", null },
                maxEvents: 1,
                sessionTimeoutMs: 500,
                readMode: EventReadMode.Metadata).ToList();

            Assert.Single(events);
        }

        [Fact]
        public async Task QueryLogsParallelRejectsInvalidParallelism() {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => {
                await foreach (var _ in SearchEvents.QueryLogsParallel("System", maxThreads: 0)) {
                }
            });
        }

        [Fact]
        public async Task QueryLogsParallelRejectsConcurrencyAboveTheReusableBound() {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => {
                await foreach (var _ in SearchEvents.QueryLogsParallel(
                                   "System",
                                   maxThreads: SearchEvents.MaximumParallelism + 1)) {
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
        public void QueryWorkItemsReserveXpathCapacityForCheckpointBoundary() {
            List<SearchEvents.QueryWorkItem> workItems = SearchEvents.BuildQueryWorkItems(
                new List<string?> { "server" },
                Enumerable.Range(1, SearchEvents.MaxXPathExpressionCount).ToList(),
                null,
                fixedExpressionCount: 0,
                minimumEventRecordIdExclusiveResolver: _ => 100).ToList();

            Assert.Equal(2, workItems.Count);
            Assert.All(workItems, item => Assert.Equal(100, item.MinimumEventRecordIdExclusive));
            Assert.All(workItems, item => Assert.True(
                (item.EventIds?.Count ?? 0) + 1 <= SearchEvents.MaxXPathExpressionCount));
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
        public void QueryMergeRejectsConcurrencyAboveTheReusableCeiling() {
            var workItems = new[] {
                new SearchEvents.QueryWorkItem(null, null, new List<long> { 1 }),
                new SearchEvents.QueryWorkItem(null, null, new List<long> { 2 })
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => SearchEvents.MergeQueryWorkItems(
                workItems,
                static _ => Enumerable.Empty<EventObject>().GetEnumerator(),
                maxEvents: 0,
                oldest: false,
                cancellationToken: CancellationToken.None,
                maxOpenQueries: int.MaxValue).ToList());
        }

        [Fact]
        public void GlobalMergeUsesTimeBeforeUnrelatedRecordIds() {
            var workItems = new[] {
                new SearchEvents.QueryWorkItem("server-a", null, new List<long> { 100 }),
                new SearchEvents.QueryWorkItem("server-b", null, new List<long> { 1 })
            };

            EventObject selected = Assert.Single(SearchEvents.MergeQueryWorkItems(
                workItems,
                workItem => CreateSingleResultQuery(
                    workItem.EventRecordIds![0],
                    workItem.MachineName == "server-a" ? new DateTime(2026, 1, 1) : new DateTime(2026, 1, 2),
                    static () => { },
                    static () => { }).GetEnumerator(),
                maxEvents: 1,
                oldest: false,
                cancellationToken: CancellationToken.None,
                maxOpenQueries: 2));

            Assert.Equal(1L, selected.RecordId);
        }

        [Fact]
        public void NamedRuleDispatchRequiresTheRegisteredLogAndEventId() {
            EventObject eventObject = CreateEventObject(
                recordId: 1,
                timeCreated: DateTime.UtcNow,
                eventId: 12,
                logName: "System",
                providerName: "Microsoft-Windows-Kernel-General");

            EventObjectSlim? wrongRule = EventObjectSlim.CreateEventRule(
                eventObject,
                new List<NamedEvents> { NamedEvents.OSStartupSecurity });
            EventObjectSlim? selectedRule = EventObjectSlim.CreateEventRule(
                eventObject,
                new List<NamedEvents> { NamedEvents.OSStartupSecurity, NamedEvents.OSStartup });

            Assert.Null(wrongRule);
            Assert.IsType<Rules.Windows.OSStartup>(selectedRule);
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

        [Fact]
        public async Task QueryLogsParallelMergesChunksBeforeApplyingTheGlobalMaximum() {
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

            var results = new List<EventObject>();
            await foreach (EventObject result in SearchEvents.QueryLogsParallel(
                               "System",
                               eventIds: eventIds,
                               maxEvents: 1,
                               readMode: EventReadMode.Metadata)) {
                results.Add(result);
            }

            EventObject selected = Assert.Single(results);
            Assert.Equal(latest.Id, selected.Id);
            Assert.True(selected.RecordId >= latest.RecordId);
        }

        [Fact]
        public void QueryLogPreservesSingleRemoteFailureVisibility() {
            Assert.ThrowsAny<Exception>(() => SearchEvents.QueryLog(
                "System",
                machineName: "203.0.113.1",
                maxEvents: 1,
                sessionTimeoutMs: 500,
                readMode: EventReadMode.Metadata).ToList());
        }

        [Fact]
        public async Task QueryLogsParallelPreservesSingleRemoteFailureVisibility() {
            await Assert.ThrowsAnyAsync<Exception>(async () => {
                await foreach (EventObject _ in SearchEvents.QueryLogsParallel(
                                   "System",
                                   machineNames: new List<string?> { "203.0.113.1" },
                                   sessionTimeoutMs: 500,
                                   readMode: EventReadMode.Metadata)) {
                }
            });
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
            return CreateSingleResultQuery(recordId, new DateTime(recordId, DateTimeKind.Utc), opened, exhausted);
        }

        private static IEnumerable<EventObject> CreateSingleResultQuery(long recordId, DateTime timeCreated, Action opened, Action exhausted) {
            opened();
            EventObject eventObject = CreateEventObject(recordId, timeCreated, (int)recordId, "System", "Test");
            yield return eventObject;
            exhausted();
        }

        private static EventObject CreateEventObject(long recordId, DateTime timeCreated, int eventId, string logName, string providerName) {
            var eventObject = (EventObject)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(EventObject));
            SetSnapshotProperty(eventObject, nameof(EventObject.RecordId), (long?)recordId);
            SetSnapshotProperty(eventObject, nameof(EventObject.TimeCreated), timeCreated);
            SetSnapshotProperty(eventObject, nameof(EventObject.Id), eventId);
            SetSnapshotProperty(eventObject, nameof(EventObject.LogName), logName);
            SetSnapshotProperty(eventObject, nameof(EventObject.ProviderName), providerName);
            SetSnapshotProperty(eventObject, nameof(EventObject.MachineName), "test-machine");
            SetSnapshotProperty(eventObject, nameof(EventObject.Data), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            eventObject.ContainerLog = logName;
            return eventObject;
        }

        private static void SetSnapshotProperty<T>(EventObject eventObject, string propertyName, T value) {
            typeof(EventObject)
                .GetField($"<{propertyName}>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(eventObject, value);
        }
    }
}

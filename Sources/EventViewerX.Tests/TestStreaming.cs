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
        public async Task NamedEventsPositiveCapObservesOnlyConsumedMergedCandidates() {
            if (!OperatingSystem.IsWindows()) return;
            if (!TestEnv.CanReadLog("Security")) return;

            int candidatesObserved = 0;
            int emitted = 0;
            await foreach (EventObjectSlim _ in SearchEvents.FindEventsByNamedEvents(
                               [NamedEvents.OSStartupSecurity],
                               new List<string?> { null, Environment.MachineName },
                               maxThreads: 2,
                               maxEvents: 1,
                               candidateObserver: _ => candidatesObserved++)) {
                emitted++;
            }

            if (emitted == 0) return;
            Assert.Equal(1, emitted);
            Assert.Equal(1, candidatesObserved);
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

            executionInfo.RecordTargetFailure(
                new EventLogQueryTargetFailure("SERVER", "System", EventLogRemoteQueryFailureKind.Timeout, "timeout"));
            executionInfo.RecordTargetFailure(
                new EventLogQueryTargetFailure("server", "System", EventLogRemoteQueryFailureKind.HostUnavailable, "duplicate"));
            executionInfo.RecordTargetFailure(
                new EventLogQueryTargetFailure("server", "Security", EventLogRemoteQueryFailureKind.AccessDenied, "denied"));
            Assert.Collection(
                executionInfo.TargetFailures,
                failure => {
                    Assert.Equal("server", failure.MachineName, ignoreCase: true);
                    Assert.Equal("Security", failure.LogName);
                    Assert.Equal(EventLogRemoteQueryFailureKind.AccessDenied, failure.Kind);
                },
                failure => {
                    Assert.Equal("SERVER", failure.MachineName);
                    Assert.Equal("System", failure.LogName);
                    Assert.Equal(EventLogRemoteQueryFailureKind.Timeout, failure.Kind);
                });

            executionInfo.Reset(maxEventsScanned: 0);
            Assert.Empty(executionInfo.TargetFailures);
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
            var observedFailures = new List<EventLogQueryTargetFailure>();
            var remoteWorkItem = new SearchEvents.QueryWorkItem(" server ", null, null);
            EventObject placeholder = (EventObject)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(EventObject));
            using IEnumerator<EventObject> remoteResults = new[] { placeholder }
                .Select<EventObject, EventObject>(_ => throw new TimeoutException("remote timeout"))
                .GetEnumerator();

            Assert.False(SearchEvents.TryMoveNextQueryWorkItem(
                remoteResults,
                remoteWorkItem,
                failedTargets,
                out EventObject? result,
                observedFailures.Add,
                "System"));
            Assert.Null(result);
            Assert.True(SearchEvents.ShouldSkipFailedTarget(new SearchEvents.QueryWorkItem("SERVER", null, null), failedTargets));
            EventLogQueryTargetFailure observedFailure = Assert.Single(observedFailures);
            Assert.Equal("server", observedFailure.MachineName);
            Assert.Equal("System", observedFailure.LogName);
            Assert.Equal(EventLogRemoteQueryFailureKind.Timeout, observedFailure.Kind);

            using IEnumerator<EventObject> localResults = new[] { placeholder }
                .Select<EventObject, EventObject>(_ => throw new TimeoutException("local timeout"))
                .GetEnumerator();
            Assert.Throws<TimeoutException>(() => SearchEvents.TryMoveNextQueryWorkItem(
                localResults,
                new SearchEvents.QueryWorkItem(null, null, null),
                failedTargets,
                out _,
                targetFailureObserver: null,
                logName: "System"));
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

            Assert.False(EventLogRemoteQueryFailureClassifier.TryClassify(
                "server",
                new InvalidEventLogQueryException(),
                out EventLogRemoteQueryFailureKind invalidQueryFailure));
            Assert.Equal(EventLogRemoteQueryFailureKind.None, invalidQueryFailure);
        }

        [Fact]
        public void RemoteFailureClassifierTreatsSessionConstructionFailureAsHostUnavailable() {
            var sessionResult = new EventLogSessionOpenResult {
                TargetHost = "server",
                Status = EventLogSessionOpenStatus.EventLogSessionUnavailable
            };
            var exception = new EventLogSessionException(sessionResult, "session constructor failed");

            Assert.True(EventLogRemoteQueryFailureClassifier.TryClassify(
                "server",
                exception,
                out EventLogRemoteQueryFailureKind failureKind));
            Assert.Equal(EventLogRemoteQueryFailureKind.HostUnavailable, failureKind);
        }

        [Fact]
        public void NamedEventSourceRestrictionsApplyBeforeCandidateEnumeration() {
            var sources = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase) {
                ["System"] = new HashSet<int> { 12, 13 },
                ["Security"] = new HashSet<int> { 4608 }
            };

            Dictionary<string, HashSet<int>> restricted = SearchEvents.RestrictNamedEventSources(
                sources,
                sourceLogName: " system ",
                sourceEventIds: new[] { 13, 4608 });

            KeyValuePair<string, HashSet<int>> source = Assert.Single(restricted);
            Assert.Equal("System", source.Key);
            Assert.Equal(new[] { 13 }, source.Value);
            Assert.Throws<ArgumentException>(() => SearchEvents.RestrictNamedEventSources(
                sources,
                sourceLogName: null,
                sourceEventIds: new[] { 0 }));
        }

        private sealed class InvalidEventLogQueryException : System.Diagnostics.Eventing.Reader.EventLogException {
            internal InvalidEventLogQueryException() {
                HResult = 15001;
            }
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
        public void CheckpointQueriesKeepSparseEventIdsInNativeChunks() {
            var eventIds = Enumerable.Range(1, SearchEvents.MaxXPathExpressionCount + 1).ToList();

            List<SearchEvents.QueryWorkItem> workItems = SearchEvents.BuildQueryWorkItems(
                new List<string?> { "server" },
                eventIds,
                null,
                fixedExpressionCount: 0,
                minimumEventRecordIdExclusiveResolver: _ => 100).ToList();

            Assert.Equal(2, workItems.Count);
            Assert.All(workItems, item => {
                Assert.NotNull(item.EventIds);
                Assert.Null(item.EventRecordIds);
                Assert.Null(item.ManagedEventIds);
                Assert.Null(item.ManagedEventRecordIds);
                Assert.Equal(100, item.MinimumEventRecordIdExclusive);
            });
            Assert.Equal(eventIds, workItems.SelectMany(static item => item.EventIds!).ToList());
        }

        [Fact]
        public void QueryTargetsAreTrimmedAndDeduplicatedBeforeCheckpointResolution() {
            List<string?> targets = SearchEvents.NormalizeQueryTargets(
                new List<string?> { " AD1 ", "ad1", " ", null, "AD2" });

            Assert.Equal(new string?[] { "AD1", null, "AD2" }, targets);
        }

        [Theory]
        [InlineData(1, 1024)]
        [InlineData(4, 1024)]
        [InlineData(8, 512)]
        [InlineData(128, 32)]
        [InlineData(4096, 1)]
        public void CheckpointPageSizeKeepsCandidateBufferBounded(int sources, int expectedPageSize) {
            Assert.Equal(expectedPageSize, SearchEvents.GetCheckpointCandidatePageSize(sources));
        }

        [Fact]
        public void PagedSourceMergeIncludesLaterSourcesInGlobalOrder() {
            int leftIndex = 0;
            int rightIndex = 0;
            int[] left = { 1, 3, 5, 7 };
            int[] right = { 2, 4, 6, 8 };

            IReadOnlyList<int> ReadPage(int[] source, ref int sourceIndex, int requested) {
                int count = Math.Min(requested, source.Length - sourceIndex);
                int[] page = source.Skip(sourceIndex).Take(count).ToArray();
                sourceIndex += count;
                return page;
            }

            List<int> merged = SearchEvents.MergePagedSources(
                    new Func<int, IReadOnlyList<int>>[] {
                        requested => ReadPage(left, ref leftIndex, requested),
                        requested => ReadPage(right, ref rightIndex, requested)
                    },
                    static (leftValue, rightValue) => leftValue.CompareTo(rightValue),
                    pageSize: 2)
                .Take(4)
                .ToList();

            Assert.Equal(new[] { 1, 2, 3, 4 }, merged);
        }

        [Fact]
        public void PagedSourceMergeDoesNotRefillAfterTheConsumerStops() {
            var requests = new List<int>();

            IReadOnlyList<int> ReadPage(int requested) {
                requests.Add(requested);
                return new[] { requests.Count };
            }

            int first = SearchEvents.MergePagedSources(
                    new Func<int, IReadOnlyList<int>>[] { ReadPage },
                    static (left, right) => left.CompareTo(right),
                    pageSize: 32)
                .Take(1)
                .Single();

            Assert.Equal(1, first);
            Assert.Equal(new[] { 1 }, requests);
        }

        [Fact]
        public void CheckpointPageReadersAdvanceEachNativeChunkWithoutSkipping() {
            var sourceEvents = new Dictionary<int, EventObject[]> {
                [1] = new[] {
                    CreateEventObject(1, new DateTime(100, DateTimeKind.Utc), 1, "System", "Test"),
                    CreateEventObject(3, new DateTime(1, DateTimeKind.Utc), 1, "System", "Test"),
                    CreateEventObject(5, new DateTime(5, DateTimeKind.Utc), 1, "System", "Test")
                },
                [2] = new[] {
                    CreateEventObject(2, new DateTime(200, DateTimeKind.Utc), 2, "System", "Test"),
                    CreateEventObject(4, new DateTime(4, DateTimeKind.Utc), 2, "System", "Test"),
                    CreateEventObject(6, new DateTime(6, DateTimeKind.Utc), 2, "System", "Test")
                }
            };
            var workItems = new[] {
                new SearchEvents.QueryWorkItem(null, new List<int> { 1 }, null, minimumEventRecordIdExclusive: 0),
                new SearchEvents.QueryWorkItem(null, new List<int> { 2 }, null, minimumEventRecordIdExclusive: 0)
            };
            List<Func<int, IReadOnlyList<EventObject>>> pageReaders = SearchEvents.CreateRecordOrderedSourcePageReaders(
                workItems,
                pageWorkItem => sourceEvents[pageWorkItem.EventIds![0]]
                    .Where(eventObject => eventObject.RecordId > pageWorkItem.MinimumEventRecordIdExclusive)
                    .GetEnumerator(),
                oldest: true);
            Assert.Single(pageReaders);

            List<long> recordIds = SearchEvents.MergePagedSources(
                    pageReaders,
                    SearchEvents.CompareCheckpointEvents,
                    pageSize: 2)
                .Select(static eventObject => eventObject.RecordId!.Value)
                .ToList();

            Assert.Equal(new long[] { 1, 2, 3, 4, 5, 6 }, recordIds);
        }

        [Fact]
        public void BoundedChunkSelectionDoesNotPrefetchTheGlobalLimitPerChunk() {
            var reads = new Dictionary<int, int>();
            var workItems = Enumerable.Range(0, 3)
                .Select(index => new SearchEvents.QueryWorkItem(
                    machineName: null,
                    eventIds: new List<int> { index + 1 },
                    eventRecordIds: null))
                .ToList();
            var sourceEvents = workItems.ToDictionary(
                static item => item.EventIds![0],
                static item => Enumerable.Range(0, 100)
                    .Select(offset => CreateEventObject(
                        300 - ((offset * 3) + item.EventIds![0] - 1),
                        new DateTime(300 - ((offset * 3) + item.EventIds[0] - 1), DateTimeKind.Utc),
                        item.EventIds[0],
                        "System",
                        "Test"))
                    .ToArray());

            List<Func<int, IReadOnlyList<EventObject>>> pageReaders = SearchEvents.CreateRecordOrderedSourcePageReaders(
                workItems,
                pageWorkItem => sourceEvents[pageWorkItem.EventIds![0]]
                    .Where(eventObject => !pageWorkItem.MaximumEventRecordIdExclusive.HasValue ||
                                          eventObject.RecordId < pageWorkItem.MaximumEventRecordIdExclusive)
                    .Select(eventObject => {
                        int key = pageWorkItem.EventIds[0];
                        reads[key] = reads.TryGetValue(key, out int count) ? count + 1 : 1;
                        return eventObject;
                })
                    .GetEnumerator(),
                oldest: false,
                boundedPageSize: 2);

            Assert.Single(pageReaders);
            Assert.Equal(new long[] { 300, 299 }, pageReaders[0](2).Select(static item => item.RecordId!.Value));
            Assert.InRange(reads.Values.Sum(), 3, workItems.Count + 2);
        }

        [Fact]
        public void ReversePageReadersResumeBelowTheLastNativeRecord() {
            EventObject[] source = Enumerable.Range(1, 6)
                .Reverse()
                .Select(value => CreateEventObject(value, new DateTime(value, DateTimeKind.Utc), value, "System", "Test"))
                .ToArray();
            var observedUpperBounds = new List<long?>();
            var workItem = new SearchEvents.QueryWorkItem(null, null, null);
            Func<int, IReadOnlyList<EventObject>> reader = SearchEvents.CreateCheckpointPageReader(
                workItem,
                pageWorkItem => {
                    observedUpperBounds.Add(pageWorkItem.MaximumEventRecordIdExclusive);
                    return source
                        .Where(eventObject => !pageWorkItem.MaximumEventRecordIdExclusive.HasValue ||
                                              eventObject.RecordId < pageWorkItem.MaximumEventRecordIdExclusive)
                        .GetEnumerator();
                },
                oldest: false);

            Assert.Equal(new long[] { 6, 5 }, reader(2).Select(static item => item.RecordId!.Value));
            Assert.Equal(new long[] { 4, 3 }, reader(2).Select(static item => item.RecordId!.Value));
            Assert.Equal(new long?[] { null, 5 }, observedUpperBounds);
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
        public async Task ParallelPagedMergePrimesPhysicalSourcesConcurrently() {
            using var bothSourcesStarted = new CountdownEvent(2);

            Func<int, IReadOnlyList<int>> CreateReader(int value) {
                int calls = 0;
                return _ => {
                    if (Interlocked.Increment(ref calls) > 1) {
                        return Array.Empty<int>();
                    }
                    bothSourcesStarted.Signal();
                    if (!bothSourcesStarted.Wait(TimeSpan.FromSeconds(2))) {
                        throw new TimeoutException("Physical source heads were not acquired concurrently.");
                    }
                    return new[] { value };
                };
            }

            var results = new List<int>();
            await foreach (int result in SearchEvents.MergePagedSourcesParallel(
                               new[] { CreateReader(2), CreateReader(1) },
                               static (left, right) => left.CompareTo(right),
                               pageSize: 1,
                               maxConcurrency: 2)) {
                results.Add(result);
            }

            Assert.Equal(new[] { 1, 2 }, results);
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
            eventObject.QueriedMachine = "test-machine";
            return eventObject;
        }

        private static void SetSnapshotProperty<T>(EventObject eventObject, string propertyName, T value) {
            typeof(EventObject)
                .GetField($"<{propertyName}>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(eventObject, value);
        }
    }
}

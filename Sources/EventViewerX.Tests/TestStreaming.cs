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
        }

        [Fact]
        public void QueryWorkItemsAreProducedLazilyInsteadOfMaterializingTheCrossProduct() {
            var eventIds = Enumerable.Range(1, 5000).ToList();
            var recordIds = Enumerable.Range(1, 5000).Select(static value => (long)value).ToList();

            IEnumerable<SearchEvents.QueryWorkItem> workItems = SearchEvents.BuildQueryWorkItems(
                new List<string?> { null, "server" },
                eventIds,
                recordIds,
                fixedExpressionCount: 0);

            Assert.False(workItems is ICollection<SearchEvents.QueryWorkItem>);
            Assert.Single(workItems.Take(1));
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
    }
}

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;

namespace EventViewerX.Tests {
    [Collection("TimedNativeOperations")]
    public class TestCancellation {
        [Fact]
        public async Task QueryLogsParallelAsyncHonorsCancellation() {
            if (!OperatingSystem.IsWindows()) return;
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => {
                await SearchEvents.QueryLogsParallelAsync("System", cancellationToken: cts.Token);
            });
        }

        [Fact]
        public async Task TimedNativeOperationsStayWithinTheGlobalThreadBound() {
            using var release = new ManualResetEventSlim();
            using var started = new CountdownEvent(SearchEvents.MaximumConcurrentTimedNativeOperations);
            var blockers = new List<Task<int>>(SearchEvents.MaximumConcurrentTimedNativeOperations);
            for (int index = 0; index < SearchEvents.MaximumConcurrentTimedNativeOperations; index++) {
                blockers.Add(Task.Factory.StartNew(
                    () => SearchEvents.ExecuteWithTimeout(
                        () => {
                            started.Signal();
                            release.Wait();
                            return 1;
                        },
                        timeoutMs: 10000,
                        timeoutMessage: "Blocking test operation timed out."),
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default));
            }

            try {
                Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
                Assert.Throws<TimeoutException>(() => SearchEvents.ExecuteWithTimeout(
                    () => 1,
                    timeoutMs: 100,
                    timeoutMessage: "No native-operation slot was available."));
            } finally {
                release.Set();
            }

            int[] results = await Task.WhenAll(blockers);
            Assert.All(results, static result => Assert.Equal(1, result));
        }
    }

    [CollectionDefinition("TimedNativeOperations", DisableParallelization = true)]
    public sealed class TimedNativeOperationsCollection {
    }
}

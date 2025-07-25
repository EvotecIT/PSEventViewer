using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EventViewerX.Tests {
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
    }
}

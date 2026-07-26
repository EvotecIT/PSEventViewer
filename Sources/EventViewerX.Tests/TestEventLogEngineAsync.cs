using System.Runtime.CompilerServices;
using Xunit;

namespace EventViewerX.Tests;

public sealed class TestEventLogEngineAsync {
    [Fact]
    public async Task CancellationDoesNotWaitForAStuckNativeProducer() {
        using var producerEntered =
            new ManualResetEventSlim();
        using var releaseProducer =
            new ManualResetEventSlim();
        using var cancellation =
            new CancellationTokenSource();

        IAsyncEnumerator<EventObject> enumerator =
            EventLogEngine.ReadAsync(
                    _ => BlockingSource(),
                    bufferCapacity: 1,
                    cancellation.Token)
                .GetAsyncEnumerator();
        try {
            Task<bool> moveNext =
                enumerator.MoveNextAsync().AsTask();
            Assert.True(
                producerEntered.Wait(
                    TimeSpan.FromSeconds(5)),
                "The producer did not enter the blocking source.");

            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<
                OperationCanceledException>(
                async () =>
                    await moveNext.WaitAsync(
                        TimeSpan.FromSeconds(5)));
        } finally {
            releaseProducer.Set();
            await enumerator.DisposeAsync();
        }

        IEnumerable<EventObject> BlockingSource() {
            producerEntered.Set();
            releaseProducer.Wait();
            yield break;
        }
    }
}
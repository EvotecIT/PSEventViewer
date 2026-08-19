using System.Runtime.CompilerServices;
using Xunit;

namespace EventViewerX.Tests;

public sealed class TestEventLogEngineAsync {
    [Fact]
    public async Task SourceRemainsLazyUntilTheFirstMove() {
        var started = false;
        IAsyncEnumerator<EventObject> enumerator =
            EventLogEngine.ReadAsync(
                    _ => Source(),
                    bufferCapacity: 1,
                    CancellationToken.None)
                .GetAsyncEnumerator();

        Assert.False(started);
        Assert.False(await enumerator.MoveNextAsync());
        Assert.True(started);
        await enumerator.DisposeAsync();

        IEnumerable<EventObject> Source() {
            started = true;
            yield break;
        }
    }

    [Fact]
    public async Task SourceFailurePropagatesToTheConsumer() {
        var expected = new InvalidOperationException("source failed");
        IAsyncEnumerator<EventObject> enumerator =
            EventLogEngine.ReadAsync(
                    _ => FailingSource(),
                    bufferCapacity: 1,
                    CancellationToken.None)
                .GetAsyncEnumerator();
        try {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await enumerator.MoveNextAsync());
            Assert.Same(expected, exception);
        } finally {
            await enumerator.DisposeAsync();
        }

        IEnumerable<EventObject> FailingSource() =>
            throw expected;
    }

    [Fact]
    public async Task CancellationDoesNotWaitForAStuckNativeProducer() {
        var producerEntered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
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
                await producerEntered.Task.WaitAsync(
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
            producerEntered.TrySetResult(true);
            releaseProducer.Wait();
            yield break;
        }
    }

    [Fact]
    public async Task StreamCancellationPreservesTheSuppliedToken() {
        var producerEntered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseProducer = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();
        IAsyncEnumerator<EventObject> enumerator =
            EventLogEngine.ReadAsync(
                    _ => BlockingSource(),
                    bufferCapacity: 1,
                    cancellation.Token)
                .GetAsyncEnumerator();
        Task<bool> moveNext = enumerator.MoveNextAsync().AsTask();
        Assert.True(
            await producerEntered.Task.WaitAsync(TimeSpan.FromSeconds(5)));

        cancellation.Cancel();

        var exception = await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            async () => await moveNext.WaitAsync(
                TimeSpan.FromSeconds(5)));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        releaseProducer.Set();
        await enumerator.DisposeAsync();

        IEnumerable<EventObject> BlockingSource() {
            producerEntered.TrySetResult(true);
            releaseProducer.Wait();
            yield break;
        }
    }

    [Fact]
    public async Task EnumerationCancellationPreservesTheSuppliedToken() {
        var producerEntered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseProducer = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();
        IAsyncEnumerator<EventObject> enumerator =
            EventLogEngine.ReadAsync(
                    _ => BlockingSource(),
                    bufferCapacity: 1,
                    CancellationToken.None)
                .GetAsyncEnumerator(cancellation.Token);
        Task<bool> moveNext = enumerator.MoveNextAsync().AsTask();
        Assert.True(
            await producerEntered.Task.WaitAsync(TimeSpan.FromSeconds(5)));

        cancellation.Cancel();

        var exception = await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            async () => await moveNext.WaitAsync(
                TimeSpan.FromSeconds(5)));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        releaseProducer.Set();
        await enumerator.DisposeAsync();

        IEnumerable<EventObject> BlockingSource() {
            producerEntered.TrySetResult(true);
            releaseProducer.Wait();
            yield break;
        }
    }

    [Fact]
    public async Task ConcurrentMoveNextAndDisposeCompleteWithoutWaitingForProducer() {
        var producerEntered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseProducer = new ManualResetEventSlim();
        IAsyncEnumerator<EventObject> enumerator =
            EventLogEngine.ReadAsync(
                    _ => BlockingSource(),
                    bufferCapacity: 1,
                    CancellationToken.None)
                .GetAsyncEnumerator();
        try {
            Task<bool> moveNext = enumerator.MoveNextAsync().AsTask();
            Assert.True(
                await producerEntered.Task.WaitAsync(
                    TimeSpan.FromSeconds(5)),
                "The producer did not enter the blocking source.");

            Task dispose = enumerator.DisposeAsync().AsTask();

            Assert.False(await moveNext.WaitAsync(TimeSpan.FromSeconds(5)));
            await dispose.WaitAsync(TimeSpan.FromSeconds(5));
        } finally {
            releaseProducer.Set();
        }

        IEnumerable<EventObject> BlockingSource() {
            producerEntered.TrySetResult(true);
            releaseProducer.Wait();
            yield break;
        }
    }

    [Fact]
    public async Task CancellationAndDisposalRemainStableAcrossRepeatedRaces() {
        for (var iteration = 0; iteration < 100; iteration++) {
            var producerEntered = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            using var releaseProducer = new ManualResetEventSlim();
            using var cancellation = new CancellationTokenSource();
            IAsyncEnumerator<EventObject> enumerator =
                EventLogEngine.ReadAsync(
                        _ => BlockingSource(),
                        bufferCapacity: 1,
                        cancellation.Token)
                    .GetAsyncEnumerator();
            try {
                Task<bool> moveNext = enumerator.MoveNextAsync().AsTask();
                Assert.True(
                    await producerEntered.Task.WaitAsync(
                        TimeSpan.FromSeconds(2)),
                    $"Producer did not start in iteration {iteration}.");

                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    async () =>
                        await moveNext.WaitAsync(
                            TimeSpan.FromSeconds(2)));
                releaseProducer.Set();
            } finally {
                releaseProducer.Set();
                await enumerator.DisposeAsync();
            }

            IEnumerable<EventObject> BlockingSource() {
                producerEntered.TrySetResult(true);
                releaseProducer.Wait();
                yield break;
            }
        }
    }
}

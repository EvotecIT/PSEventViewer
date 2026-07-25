using Xunit;
using EventViewerX.Native;
using System.Collections.Concurrent;

namespace EventViewerX.Tests;

public sealed class TestRemoteConnectionBudget {
    [Fact]
    public void RpcProbeHonorsCallerCancellation() {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            Native.RpcEndpointProbe.TryConnect(
                "192.0.2.1",
                135,
                30000,
                cancellation.Token));
    }

    [Fact]
    public void NativeOperationAdmissionHonorsCallerCancellation() {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            Native.BoundedNativeOperation.Acquire(
                30000,
                "timed out",
                cancellation.Token));
    }

    [Fact]
    public void SubtractsCompletedSetupStages() {
        var budget = System.Diagnostics.Stopwatch.StartNew();
        Thread.Sleep(25);

        int remaining =
            Native.WindowsEventRemoteReader
                .GetRemainingConnectionTimeout(
                    budget,
                    1000,
                    "timed out");

        Assert.InRange(remaining, 1, 999);
    }

    [Fact]
    public void FailsBeforeStartingTheNextStageWhenExhausted() {
        var budget = System.Diagnostics.Stopwatch.StartNew();
        Thread.Sleep(20);

        Assert.Throws<TimeoutException>(() =>
            Native.WindowsEventRemoteReader
                .GetRemainingConnectionTimeout(
                    budget,
                    1,
                    "timed out"));
    }

    [Fact]
    public void BufferedRemoteProducersReleaseNativeSlotsWhileBackpressured() {
        const int producerCount =
            BoundedNativeOperation.MaximumConcurrentOperations;
        using var cancellation =
            new CancellationTokenSource();
        var buffers =
            Enumerable.Range(0, producerCount)
                .Select(_ =>
                    new BlockingCollection<EventObject>(1))
                .ToArray();
        Task[] producers =
            buffers.Select(buffer =>
                    Task.Run(() => {
                        using IEnumerator<EventObject> events =
                            Enumerable.Range(0, 3)
                                .Select(index =>
                                    CreateEvent(index))
                                .GetEnumerator();
                        try {
                            WindowsEventRemoteReader.CopyToBuffer(
                                events,
                                buffer,
                                maxEvents: 0,
                                readTimeoutMilliseconds: 1000,
                                slotTimeoutMessage:
                                    "Timed out waiting for a native read slot.",
                                cancellation.Token);
                        } catch (OperationCanceledException)
                            when (cancellation.IsCancellationRequested) {
                        }
                    }))
                .ToArray();
        try {
            Assert.True(
                SpinWait.SpinUntil(
                    () => buffers.All(
                        static buffer =>
                            buffer.Count == 1),
                    TimeSpan.FromSeconds(5)),
                "The remote producers did not reach buffer backpressure.");

            using IDisposable seventeenth =
                BoundedNativeOperation.Acquire(
                    1000,
                    "Backpressured readers retained all native slots.");
        } finally {
            cancellation.Cancel();
            Assert.True(
                Task.WaitAll(
                    producers,
                    TimeSpan.FromSeconds(5)),
                "The backpressured producers did not stop after cancellation.");
            foreach (BlockingCollection<EventObject> buffer in buffers) {
                buffer.Dispose();
            }
        }
    }

    private static EventObject CreateEvent(
        int id) {

        var metadata = new NativeEventMetadata(
            "EventViewerX.Tests",
            providerId: null,
            id,
            qualifiers: null,
            level: 4,
            task: null,
            opcode: null,
            keywords: null,
            timeCreated: DateTime.UtcNow,
            recordId: id,
            activityId: null,
            relatedActivityId: null,
            processId: null,
            threadId: null,
            logName: "Application",
            machineName: Environment.MachineName,
            userId: null,
            version: null);
        return new EventObject(
            metadata,
            Environment.MachineName,
            "Application");
    }
}

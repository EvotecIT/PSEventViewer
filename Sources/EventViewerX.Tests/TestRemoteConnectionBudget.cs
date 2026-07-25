using Xunit;

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
}

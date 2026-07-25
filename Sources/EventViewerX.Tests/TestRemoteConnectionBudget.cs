using Xunit;

namespace EventViewerX.Tests;

public sealed class TestRemoteConnectionBudget {
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

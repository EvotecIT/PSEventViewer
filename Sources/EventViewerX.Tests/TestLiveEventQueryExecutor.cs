using EventViewerX.Reports.Live;
using Xunit;

namespace EventViewerX.Tests;

public class TestLiveEventQueryExecutor {
    [Fact]
    public void TryRead_ShouldFailForMissingLogName() {
        var ok = LiveEventQueryExecutor.TryRead(
            request: new LiveEventQueryRequest {
                LogName = string.Empty
            },
            result: out _,
            failure: out var failure);

        Assert.False(ok);
        Assert.NotNull(failure);
        Assert.Equal(LiveEventQueryFailureKind.InvalidArgument, failure!.Kind);
    }

    [Fact]
    public void TryRead_ShouldFailForNegativeMaxEvents() {
        var ok = LiveEventQueryExecutor.TryRead(
            request: new LiveEventQueryRequest {
                LogName = "Application",
                MaxEvents = -1
            },
            result: out _,
            failure: out var failure);

        Assert.False(ok);
        Assert.NotNull(failure);
        Assert.Equal(LiveEventQueryFailureKind.InvalidArgument, failure!.Kind);
    }
}

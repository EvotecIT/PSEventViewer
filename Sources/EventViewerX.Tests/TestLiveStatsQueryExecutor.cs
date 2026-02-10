using System;
using EventViewerX.Reports.Live;
using Xunit;

namespace EventViewerX.Tests;

public class TestLiveStatsQueryExecutor {
    [Fact]
    public void TryBuild_ShouldFailForMissingLogName() {
        var ok = LiveStatsQueryExecutor.TryBuild(
            request: new LiveStatsQueryRequest {
                LogName = string.Empty
            },
            result: out _,
            failure: out var failure);

        Assert.False(ok);
        Assert.NotNull(failure);
        Assert.Equal(LiveStatsQueryFailureKind.InvalidArgument, failure!.Kind);
    }

    [Fact]
    public void TryBuild_ShouldFailForInvalidTimeRange() {
        var ok = LiveStatsQueryExecutor.TryBuild(
            request: new LiveStatsQueryRequest {
                LogName = "Security",
                StartTimeUtc = new DateTime(2026, 2, 10, 11, 0, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 2, 10, 10, 0, 0, DateTimeKind.Utc)
            },
            result: out _,
            failure: out var failure);

        Assert.False(ok);
        Assert.NotNull(failure);
        Assert.Equal(LiveStatsQueryFailureKind.InvalidArgument, failure!.Kind);
    }
}

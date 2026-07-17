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

    [Fact]
    public void BuildEffectiveXPath_AppliesUtcRangeToCustomFilter() {
        var start = new DateTime(2026, 2, 10, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(1);

        string xpath = LiveStatsQueryExecutor.BuildEffectiveXPath("*[System[EventID=16]]", start, end);

        Assert.Contains("(*[System[EventID=16]]) and", xpath, StringComparison.Ordinal);
        Assert.Contains("2026-02-10T10:00:00.0000000Z", xpath, StringComparison.Ordinal);
        Assert.Contains("2026-02-10T11:00:00.0000000Z", xpath, StringComparison.Ordinal);
    }
}

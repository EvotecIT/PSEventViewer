using EventViewerX.Reports.Correlation;
using Xunit;

namespace EventViewerX.Tests;

public class TestNamedEventsTimelineQueryExecutor {
    [Fact]
    public async Task TryBuildAsync_ShouldFailWhenNamedEventsMissing() {
        var (result, failure) = await NamedEventsTimelineQueryExecutor.TryBuildAsync(
            new NamedEventsTimelineQueryRequest());

        Assert.Null(result);
        Assert.NotNull(failure);
        Assert.Equal(NamedEventsTimelineQueryFailureKind.InvalidArgument, failure!.Kind);
        Assert.Contains("namedEvents", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TryBuildAsync_ShouldFailWhenTimeRangeInvalid() {
        var (result, failure) = await NamedEventsTimelineQueryExecutor.TryBuildAsync(
            new NamedEventsTimelineQueryRequest {
                NamedEvents = new[] { NamedEvents.ADUserLogon },
                StartTimeUtc = new DateTime(2026, 2, 20, 11, 0, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 2, 20, 10, 0, 0, DateTimeKind.Utc)
            });

        Assert.Null(result);
        Assert.NotNull(failure);
        Assert.Equal(NamedEventsTimelineQueryFailureKind.InvalidArgument, failure!.Kind);
        Assert.Contains("startTimeUtc", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TryBuildAsync_ShouldFailWhenTimePeriodCombinedWithRange() {
        var (result, failure) = await NamedEventsTimelineQueryExecutor.TryBuildAsync(
            new NamedEventsTimelineQueryRequest {
                NamedEvents = new[] { NamedEvents.ADUserLogon },
                TimePeriod = TimePeriod.Last1Hour,
                StartTimeUtc = new DateTime(2026, 2, 20, 10, 0, 0, DateTimeKind.Utc)
            });

        Assert.Null(result);
        Assert.NotNull(failure);
        Assert.Equal(NamedEventsTimelineQueryFailureKind.InvalidArgument, failure!.Kind);
        Assert.Contains("timePeriod", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TryBuildAsync_ShouldFailWhenCorrelationKeyInvalid() {
        var (result, failure) = await NamedEventsTimelineQueryExecutor.TryBuildAsync(
            new NamedEventsTimelineQueryRequest {
                NamedEvents = new[] { NamedEvents.ADUserLogon },
                CorrelationKeys = new[] { "invalid_dimension" }
            });

        Assert.Null(result);
        Assert.NotNull(failure);
        Assert.Equal(NamedEventsTimelineQueryFailureKind.InvalidArgument, failure!.Kind);
        Assert.Contains("correlationKeys", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TryBuildAsync_ShouldFailWhenEventIdsContainNonPositiveValues() {
        var (result, failure) = await NamedEventsTimelineQueryExecutor.TryBuildAsync(
            new NamedEventsTimelineQueryRequest {
                NamedEvents = new[] { NamedEvents.ADUserLogon },
                EventIds = new[] { 4624, 0 }
            });

        Assert.Null(result);
        Assert.NotNull(failure);
        Assert.Equal(NamedEventsTimelineQueryFailureKind.InvalidArgument, failure!.Kind);
        Assert.Contains("eventIds", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryParseUtcValue_ShouldTreatUnspecifiedTimestampAsUtc() {
        var parsed = NamedEventsTimelineQueryExecutor.TryParseUtcValue("2026-02-20T12:34:56", out var utc);

        Assert.True(parsed);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(new DateTime(2026, 2, 20, 12, 34, 56, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void TryParseUtcValue_ShouldConvertOffsetTimestampToUtc() {
        var parsed = NamedEventsTimelineQueryExecutor.TryParseUtcValue("2026-02-20T12:34:56+02:00", out var utc);

        Assert.True(parsed);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(new DateTime(2026, 2, 20, 10, 34, 56, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void TryParseUtcValue_ShouldReturnFalseForInvalidInput() {
        var parsed = NamedEventsTimelineQueryExecutor.TryParseUtcValue("not-a-timestamp", out _);

        Assert.False(parsed);
    }
}

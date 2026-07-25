using Xunit;

namespace EventViewerX.Tests;

public sealed class TestNamedEventEngine {
    [Fact]
    public async Task EmptyRestrictedQueryResetsReusableExecutionInfo() {
        var executionInfo = new NamedEventsQueryExecutionInfo();
        executionInfo.Reset(maxEventsScanned: 1);
        Assert.True(executionInfo.TryRecordCandidate());
        executionInfo.EventsEmitted = 1;
        executionInfo.RecordTargetFailure(
            new EventLogQueryTargetFailure(
                "remote.example.test",
                "Security",
                EventLogRemoteQueryFailureKind.AccessDenied,
                "Access denied."));
        var query = new NamedEventQuery(
            new[] { NamedEvents.ADUserLogon }) {
            SourceLogName = "EventViewerX-Missing-Channel",
            MaxCandidates = 7
        };

        await foreach (EventObjectSlim _ in
                       NamedEventEngine.ReadAsync(
                           query,
                           executionInfo)) {
            Assert.Fail(
                "A query restricted to an unrelated channel must be empty.");
        }

        Assert.Equal(0, executionInfo.EventsScanned);
        Assert.Equal(0, executionInfo.EventsEmitted);
        Assert.Equal(7, executionInfo.MaxEventsScanned);
        Assert.False(executionInfo.ScanLimitReached);
        Assert.Empty(executionInfo.TargetFailures);
    }

    [Fact]
    public void RemoteFailureIsRecordedOnlyForTheFailedChannel() {
        var executionInfo = new NamedEventsQueryExecutionInfo();
        executionInfo.Reset(maxEventsScanned: 0);
        var failure = new EventLogQueryFailure(
            source: "Security",
            machineName: "remote.example.test",
            exception: new UnauthorizedAccessException("Access denied."));

        NamedEventEngine.HandleFailure(
            failure,
            executionInfo);

        EventLogQueryTargetFailure recorded =
            Assert.Single(executionInfo.TargetFailures);
        Assert.Equal("remote.example.test", recorded.MachineName);
        Assert.Equal("Security", recorded.LogName);
        Assert.Equal(
            EventLogRemoteQueryFailureKind.AccessDenied,
            recorded.Kind);
    }
}

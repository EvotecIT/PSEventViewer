using Xunit;

namespace EventViewerX.Tests;

public sealed class TestNamedEventEngine {
    [Fact]
    public async Task EmptyRestrictedQueryResetsReusableExecutionInfo() {
        var executionInfo = new NamedEventsQueryExecutionInfo();
        executionInfo.Reset(maxEventsScanned: 1);
        var candidateCounter =
            new NamedEventCandidateCounter(
                maxEventsScanned: 1,
                executionInfo);
        Assert.True(
            candidateCounter.TryRecordCandidate());
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
    public void CandidateCapsRemainLocalWhenExecutionInfoIsReused() {
        var executionInfo =
            new NamedEventsQueryExecutionInfo();
        executionInfo.Reset(maxEventsScanned: 1);
        var capped =
            new NamedEventCandidateCounter(
                maxEventsScanned: 1,
                executionInfo);

        Assert.True(capped.TryRecordCandidate());

        executionInfo.Reset(maxEventsScanned: 0);
        var unlimited =
            new NamedEventCandidateCounter(
                maxEventsScanned: 0,
                executionInfo);

        Assert.True(unlimited.TryRecordCandidate());
        Assert.True(unlimited.TryRecordCandidate());
        Assert.False(capped.TryRecordCandidate());
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

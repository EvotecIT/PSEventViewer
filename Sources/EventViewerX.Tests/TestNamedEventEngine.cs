using Xunit;

namespace EventViewerX.Tests;

public sealed class TestNamedEventEngine {
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

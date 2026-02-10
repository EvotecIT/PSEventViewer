using EventViewerX.Reports.Evtx;
using Xunit;

namespace EventViewerX.Tests;

public class TestEvtxEventReportBuilder {
    [Fact]
    public void TryBuild_ShouldFailForNegativeMaxMessageChars() {
        var ok = EvtxEventReportBuilder.TryBuild(
            request: new EvtxQueryRequest { FilePath = "dummy.evtx" },
            includeMessage: true,
            maxMessageChars: -1,
            report: out _,
            failure: out var failure);

        Assert.False(ok);
        Assert.NotNull(failure);
        Assert.Equal(EvtxQueryFailureKind.InvalidArgument, failure!.Kind);
    }

    [Fact]
    public void TryBuild_ShouldReturnNotFoundForMissingFile() {
        var ok = EvtxEventReportBuilder.TryBuild(
            request: new EvtxQueryRequest { FilePath = "C:/this/file/does/not/exist.evtx" },
            includeMessage: false,
            maxMessageChars: 1024,
            report: out _,
            failure: out var failure);

        Assert.False(ok);
        Assert.NotNull(failure);
        Assert.Equal(EvtxQueryFailureKind.NotFound, failure!.Kind);
    }
}

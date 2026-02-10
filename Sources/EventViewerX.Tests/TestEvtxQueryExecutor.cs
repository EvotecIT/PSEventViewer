using System;
using EventViewerX.Reports.Evtx;
using EventViewerX.Reports.Security;
using Xunit;

namespace EventViewerX.Tests;

public class TestEvtxQueryExecutor {
    [Fact]
    public void TryRead_ShouldFailForMissingFilePath() {
        var request = new EvtxQueryRequest {
            FilePath = string.Empty
        };

        var ok = EvtxQueryExecutor.TryRead(request, out _, out var failure);

        Assert.False(ok);
        Assert.NotNull(failure);
        Assert.Equal(EvtxQueryFailureKind.InvalidArgument, failure!.Kind);
    }

    [Fact]
    public void TryRead_ShouldFailForInvalidTimeRange() {
        var request = new EvtxQueryRequest {
            FilePath = "dummy.evtx",
            StartTimeUtc = new DateTime(2026, 2, 10, 11, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 2, 10, 10, 0, 0, DateTimeKind.Utc)
        };

        var ok = EvtxQueryExecutor.TryRead(request, out _, out var failure);

        Assert.False(ok);
        Assert.NotNull(failure);
        Assert.Equal(EvtxQueryFailureKind.InvalidArgument, failure!.Kind);
    }

    [Fact]
    public void TryRead_ShouldFailForInvalidEventIds() {
        var request = new EvtxQueryRequest {
            FilePath = "dummy.evtx",
            EventIds = new[] { 4624, -1 }
        };

        var ok = EvtxQueryExecutor.TryRead(request, out _, out var failure);

        Assert.False(ok);
        Assert.NotNull(failure);
        Assert.Equal(EvtxQueryFailureKind.InvalidArgument, failure!.Kind);
    }

    [Fact]
    public void TryRead_ShouldReturnNotFoundForMissingFile() {
        var request = new EvtxQueryRequest {
            FilePath = "C:/this/file/does/not/exist.evtx"
        };

        var ok = EvtxQueryExecutor.TryRead(request, out _, out var failure);

        Assert.False(ok);
        Assert.NotNull(failure);
        Assert.Equal(EvtxQueryFailureKind.NotFound, failure!.Kind);
    }

    [Fact]
    public void SecurityBuilder_TryBuildFromFile_ShouldSurfaceQueryFailure() {
        var request = new EvtxQueryRequest {
            FilePath = "C:/this/file/does/not/exist.evtx",
            EventIds = new[] { 4625 },
            ProviderName = "Microsoft-Windows-Security-Auditing"
        };

        var ok = SecurityFailedLogonsReportBuilder.TryBuildFromFile(
            request,
            includeSamples: false,
            sampleSize: 10,
            report: out _,
            failure: out var failure);

        Assert.False(ok);
        Assert.NotNull(failure);
        Assert.Equal(EvtxQueryFailureKind.NotFound, failure!.Kind);
    }
}

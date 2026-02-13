using EventViewerX.Reports;
using EventViewerX.Reports.Evtx;
using EventViewerX.Reports.Inventory;
using EventViewerX.Reports.Live;
using Xunit;

namespace EventViewerX.Tests;

public class TestEventViewerFailureContractResolver {
    [Fact]
    public void Resolve_EvtxIoError_IsRecoverableIoError() {
        var contract = EventViewerFailureContractResolver.Resolve(EvtxQueryFailureKind.IoError);

        Assert.Equal("io_error", contract.ErrorCode);
        Assert.Equal("io_error", contract.Category);
        Assert.True(contract.Recoverable);
        Assert.Equal("event_log_query", contract.Entity);
    }

    [Fact]
    public void Resolve_LiveEventTimeout_IsRecoverableTimeout() {
        var contract = EventViewerFailureContractResolver.Resolve(LiveEventQueryFailureKind.Timeout);

        Assert.Equal("timeout", contract.ErrorCode);
        Assert.Equal("timeout", contract.Category);
        Assert.True(contract.Recoverable);
        Assert.Equal("event_log_query", contract.Entity);
    }

    [Fact]
    public void Resolve_LiveStatsInvalidArgument_IsNonRecoverable() {
        var contract = EventViewerFailureContractResolver.Resolve(LiveStatsQueryFailureKind.InvalidArgument);

        Assert.Equal("invalid_argument", contract.ErrorCode);
        Assert.Equal("invalid_argument", contract.Category);
        Assert.False(contract.Recoverable);
        Assert.Equal("event_log_query", contract.Entity);
    }

    [Fact]
    public void Resolve_CatalogAccessDenied_IsNonRecoverable() {
        var contract = EventViewerFailureContractResolver.Resolve(EventCatalogFailureKind.AccessDenied);

        Assert.Equal("access_denied", contract.ErrorCode);
        Assert.Equal("access_denied", contract.Category);
        Assert.False(contract.Recoverable);
        Assert.Equal("event_log_query", contract.Entity);
    }

    [Fact]
    public void Resolve_CatalogException_FallsBackToQueryFailed() {
        var contract = EventViewerFailureContractResolver.Resolve(EventCatalogFailureKind.Exception);

        Assert.Equal("query_failed", contract.ErrorCode);
        Assert.Equal("query_failed", contract.Category);
        Assert.True(contract.Recoverable);
        Assert.Equal("event_log_query", contract.Entity);
    }
}

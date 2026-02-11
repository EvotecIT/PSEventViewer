using EventViewerX.Reports;
using EventViewerX.Reports.Evtx;
using EventViewerX.Reports.Inventory;
using EventViewerX.Reports.Live;
using Xunit;

namespace EventViewerX.Tests;

public class TestEventViewerToolFailureContractResolver {
    [Fact]
    public void Resolve_EvtxIoError_IsRecoverableIoError() {
        var contract = EventViewerToolFailureContractResolver.Resolve(EvtxQueryFailureKind.IoError);

        Assert.Equal("io_error", contract.ErrorCode);
        Assert.Equal("io_error", contract.Category);
        Assert.True(contract.Recoverable);
        Assert.Equal("event_log_query", contract.Entity);
    }

    [Fact]
    public void Resolve_LiveEventTimeout_IsRecoverableTimeout() {
        var contract = EventViewerToolFailureContractResolver.Resolve(LiveEventQueryFailureKind.Timeout);

        Assert.Equal("timeout", contract.ErrorCode);
        Assert.Equal("timeout", contract.Category);
        Assert.True(contract.Recoverable);
        Assert.Equal("event_log_query", contract.Entity);
    }

    [Fact]
    public void Resolve_LiveStatsInvalidArgument_IsNonRecoverable() {
        var contract = EventViewerToolFailureContractResolver.Resolve(LiveStatsQueryFailureKind.InvalidArgument);

        Assert.Equal("invalid_argument", contract.ErrorCode);
        Assert.Equal("invalid_argument", contract.Category);
        Assert.False(contract.Recoverable);
        Assert.Equal("event_log_query", contract.Entity);
    }

    [Fact]
    public void Resolve_CatalogAccessDenied_IsNonRecoverable() {
        var contract = EventViewerToolFailureContractResolver.Resolve(EventCatalogFailureKind.AccessDenied);

        Assert.Equal("access_denied", contract.ErrorCode);
        Assert.Equal("access_denied", contract.Category);
        Assert.False(contract.Recoverable);
        Assert.Equal("event_log_query", contract.Entity);
    }
}

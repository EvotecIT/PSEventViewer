using EventViewerX.Reports;
using EventViewerX.Reports.Evtx;
using EventViewerX.Reports.Inventory;
using EventViewerX.Reports.Live;
using Xunit;

namespace EventViewerX.Tests;

public class TestEventViewerFailureDescriptorResolver {
    [Fact]
    public void Resolve_EvtxIoError_IsRecoverableIoError() {
        var descriptor = EventViewerFailureDescriptorResolver.Resolve(EvtxQueryFailureKind.IoError);

        Assert.Equal("io_error", descriptor.ErrorCode);
        Assert.Equal("io_error", descriptor.Category);
        Assert.True(descriptor.Recoverable);
        Assert.Equal("event_log_query", descriptor.Entity);
    }

    [Fact]
    public void Resolve_LiveEventTimeout_IsRecoverableTimeout() {
        var descriptor = EventViewerFailureDescriptorResolver.Resolve(LiveEventQueryFailureKind.Timeout);

        Assert.Equal("timeout", descriptor.ErrorCode);
        Assert.Equal("timeout", descriptor.Category);
        Assert.True(descriptor.Recoverable);
        Assert.Equal("event_log_query", descriptor.Entity);
    }

    [Fact]
    public void Resolve_LiveStatsInvalidArgument_IsNonRecoverable() {
        var descriptor = EventViewerFailureDescriptorResolver.Resolve(LiveStatsQueryFailureKind.InvalidArgument);

        Assert.Equal("invalid_argument", descriptor.ErrorCode);
        Assert.Equal("invalid_argument", descriptor.Category);
        Assert.False(descriptor.Recoverable);
        Assert.Equal("event_log_query", descriptor.Entity);
    }

    [Fact]
    public void Resolve_CatalogAccessDenied_IsNonRecoverable() {
        var descriptor = EventViewerFailureDescriptorResolver.Resolve(EventCatalogFailureKind.AccessDenied);

        Assert.Equal("access_denied", descriptor.ErrorCode);
        Assert.Equal("access_denied", descriptor.Category);
        Assert.False(descriptor.Recoverable);
        Assert.Equal("event_log_query", descriptor.Entity);
    }

    [Fact]
    public void Resolve_CatalogException_FallsBackToQueryFailed() {
        var descriptor = EventViewerFailureDescriptorResolver.Resolve(EventCatalogFailureKind.Exception);

        Assert.Equal("query_failed", descriptor.ErrorCode);
        Assert.Equal("query_failed", descriptor.Category);
        Assert.True(descriptor.Recoverable);
        Assert.Equal("event_log_query", descriptor.Entity);
    }
}

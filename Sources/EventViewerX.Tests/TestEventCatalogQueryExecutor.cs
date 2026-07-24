using EventViewerX.Reports.Inventory;
using Xunit;

namespace EventViewerX.Tests;

public class TestEventCatalogQueryExecutor {
    [Fact]
    public void TryListChannels_ShouldFailForNullRequest() {
        var ok = EventCatalogQueryExecutor.TryListChannels(
            request: null!,
            result: out _,
            failure: out var failure);

        Assert.False(ok);
        Assert.NotNull(failure);
        Assert.Equal(EventCatalogFailureKind.InvalidArgument, failure!.Kind);
    }

    [Fact]
    public void TryListProviders_ShouldFailForNegativeMaxResults() {
        var ok = EventCatalogQueryExecutor.TryListProviders(
            request: new EventCatalogQueryRequest { MaxResults = -1 },
            result: out _,
            failure: out var failure);

        Assert.False(ok);
        Assert.NotNull(failure);
        Assert.Equal(EventCatalogFailureKind.InvalidArgument, failure!.Kind);
    }

    [Fact]
    public void TryListChannels_ShouldPreserveCallerCancellation() {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => EventCatalogQueryExecutor.TryListChannels(
            request: new EventCatalogQueryRequest(),
            result: out _,
            failure: out _,
            cancellationToken: cancellation.Token));
    }

    [Fact]
    public void TryListChannels_ShouldPreserveNegativeCacheAsHostUnavailable() {
        if (!OperatingSystem.IsWindows()) return;

        const string host = "eventviewerx-catalog-unavailable.invalid";
        EventLogSessionManager.ClearHostCache(host);
        try {
            using EventLogSessionOpenResult seed = EventLogSessionManager.CreateSessionResult(
                host,
                "CatalogTest",
                "*",
                timeoutMs: 100,
                rpcProbeOverride: static (_, _) => false);
            Assert.Equal(EventLogSessionOpenStatus.RpcUnavailable, seed.Status);

            bool success = EventCatalogQueryExecutor.TryListChannels(
                request: new EventCatalogQueryRequest { MachineName = host, SessionTimeoutMs = 100 },
                result: out _,
                failure: out EventCatalogFailure? failure);

            Assert.False(success);
            Assert.NotNull(failure);
            Assert.Equal(EventCatalogFailureKind.HostUnavailable, failure!.Kind);
        } finally {
            EventLogSessionManager.ClearHostCache(host);
        }
    }

    [Fact]
    public void BuildNameRows_ReportsTruncationOnlyWhenAnotherMatchExists() {
        var exactRequest = new EventCatalogQueryRequest { MaxResults = 2 };
        List<string> exactRows = EventCatalogQueryExecutor.BuildNameRows(
            new[] { "B", "A" },
            exactRequest,
            CancellationToken.None,
            static name => name,
            out bool exactTruncated);

        Assert.Equal(new[] { "A", "B" }, exactRows);
        Assert.False(exactTruncated);

        exactRequest.MaxResults = 1;
        List<string> cappedRows = EventCatalogQueryExecutor.BuildNameRows(
            new[] { "B", "A" },
            exactRequest,
            CancellationToken.None,
            static name => name,
            out bool cappedTruncated);

        Assert.Equal(new[] { "A" }, cappedRows);
        Assert.True(cappedTruncated);
    }
}

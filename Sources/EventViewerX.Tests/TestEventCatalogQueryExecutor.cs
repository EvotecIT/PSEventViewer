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
}

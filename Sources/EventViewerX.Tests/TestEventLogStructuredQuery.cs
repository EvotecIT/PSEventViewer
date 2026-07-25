using Xunit;

namespace EventViewerX.Tests;

public class TestEventLogStructuredQuery {
    [Fact]
    public void CountsDistinctOfflineFilesAsIndependentSources() {
        string first = Path.GetFullPath("first.evtx");
        string second = Path.GetFullPath("second.evtx");
        EventLogStructuredQuery query =
            EventLogStructuredQuery.ForFiles(
                new[] {
                    first,
                    second
                });

        Assert.Equal(
            2,
            query.GetIndependentSourceCount());
    }

    [Fact]
    public void CountsMultipleChannelsAsOneNativeSource() {
        EventLogStructuredQuery query =
            EventLogStructuredQuery.ForChannels(
                new[] {
                    "Application",
                    "System"
                });

        Assert.Equal(
            1,
            query.GetIndependentSourceCount());
    }

    [Fact]
    public void MultiSourceStructuredReadRejectsOneBookmarkBeforeSplitting() {
        EventLogStructuredQuery query =
            EventLogStructuredQuery.ForFiles(
                new[] {
                    Path.GetFullPath("first.evtx"),
                    Path.GetFullPath("second.evtx")
                });
        query.BookmarkXml = "<BookmarkList />";

        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                EventLogEngine
                    .ReadStructured(query)
                    .ToArray());

        Assert.Contains(
            "one independent structured-query source",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }
}

using Xunit;

namespace EventViewerX.Tests;

public sealed class TestEventLogStructuredQuerySources {
    [Fact]
    public void ResolvesDistinctCheckpointSources() {
        string filePath = Path.GetFullPath(
            Path.Combine(
                Path.GetTempPath(),
                "structured-source.evtx"));
        string fileUri = new Uri(filePath).AbsoluteUri;
        var query = new EventLogStructuredQuery(
            "<QueryList>" +
            "<Query Id=\"0\" Path=\"System\">" +
            "<Select Path=\"System\">*</Select>" +
            "<Suppress Path=\"Application\">*</Suppress>" +
            "</Query>" +
            $"<Query Id=\"1\" Path=\"{fileUri}\">" +
            $"<Select Path=\"{fileUri}\">*</Select>" +
            "</Query>" +
            "</QueryList>");

        EventLogStructuredQuerySource[] sources =
            query.ResolveSources().ToArray();

        Assert.Equal(3, sources.Length);
        Assert.Contains(
            sources,
            source => source.Kind ==
                      EventLogQuerySourceKind.Channel &&
                      source.Source == "System");
        Assert.Contains(
            sources,
            source => source.Kind ==
                      EventLogQuerySourceKind.Channel &&
                      source.Source == "Application");
        Assert.Contains(
            sources,
            source => source.Kind ==
                      EventLogQuerySourceKind.File &&
                      string.Equals(
                          source.Source,
                          filePath,
                          StringComparison.OrdinalIgnoreCase));
    }
}

using Xunit;

namespace EventViewerX.Tests;

public class TestPublicApiBoundary {
    [Fact]
    public void ReportingImplementation_IsNotExported() {
        string[] exportedReportingTypes = typeof(EventLogEngine)
            .Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace?.StartsWith("EventViewerX.Reports", StringComparison.Ordinal) == true)
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(exportedReportingTypes);
    }

    [Fact]
    public void CoreQueryEntryPoints_RemainExported() {
        Type[] supportedEntryPoints = {
            typeof(EventLogEngine),
            typeof(EventQueryDefinition),
            typeof(NamedEventEngine),
            typeof(NamedEventQuery),
            typeof(NamedEvents)
        };

        Assert.All(supportedEntryPoints, type => Assert.True(type.IsPublic, type.FullName));
    }
}

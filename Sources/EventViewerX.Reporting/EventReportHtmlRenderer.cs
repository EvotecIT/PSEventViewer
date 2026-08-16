using HtmlForgeX;

namespace EventViewerX.Reporting;

/// <summary>Renders an interactive, self-contained HtmlForgeX event report.</summary>
public static class EventReportHtmlRenderer {
    /// <summary>Renders an event report to an HTML string.</summary>
    public static string Render(EventReport report) {
        if (report == null) {
            throw new ArgumentNullException(nameof(report));
        }
        using var document = new Document {
            LibraryMode = LibraryMode.Offline,
            ThemeMode = ThemeMode.System,
            DarkThemeVariant = HfxThemeVariant.DarkCarbon
        };
        document.Head.Title = report.Title;
        var categoryChart = new TablerCategoryBarChartCard()
            .Title("Events by type")
            .Subtitle("The most frequent typed projections in this result")
            .Accent(TablerColor.Indigo);
        foreach (IGrouping<string, EventReportRow> group in report.Rows
                     .GroupBy(static row => row.Type, StringComparer.OrdinalIgnoreCase)
                     .OrderByDescending(static group => group.Count())
                     .Take(12)) {
            categoryChart.AddItem(group.Key, group.Count());
        }
        List<Dictionary<string, object?>> rows = EventReportTableProjection.Project(report.Rows);
        List<object> coverage = report.Coverage.Cast<object>().ToList();
        var eventsCard = new TablerDataTableCard()
            .Title("Events")
            .Subtitle("Search, sort, filter, and inspect every normalized event")
            .Accent(TablerColor.Blue)
            .Bind(rows.Cast<object>());
        var coverageCard = new TablerDataTableCard()
            .Title("Coverage")
            .Subtitle("Every queried computer and source channel")
            .Accent(report.Coverage.All(static item => item.Succeeded) ? TablerColor.Green : TablerColor.Yellow)
            .Bind(coverage);
        document.Body.Add(new TablerReportWorkspace()
            .Title("EventViewerX")
            .BrandIcon(TablerIconType.Activity, TablerColor.Indigo)
            .Settings(settings => settings
                .Density(TablerReportWorkspaceDensity.Compact)
                .Frame(TablerReportWorkspaceFrame.FullBleed)
                .End())
            .GlobalRail(rail => rail
                .Item("Overview", TablerIconType.Dashboard, active: true)
                .Item("Events", TablerIconType.ListDetails))
            .Navigation(navigation => navigation
                .Item("Report", TablerIconType.ReportAnalytics, active: true)
                .Item("Coverage", TablerIconType.Server))
            .TopBar(top => top
                .Breadcrumb("EventViewerX", report.Title)
                .ThemeSelector())
            .Content(content => content
                .Hero(hero => hero
                    .Title(report.Title)
                    .Chip("Windows Event Log")
                    .Illustration(TablerIconType.Activity))
                .Metrics(metrics => metrics
                    .Metric("Events", report.Rows.Count.ToString("N0"), TablerIconType.ListDetails, TablerColor.Blue)
                    .Metric("Sources", report.Coverage.Count.ToString("N0"), TablerIconType.Server, TablerColor.Indigo)
                    .Metric("Query", $"{report.QueryDuration.TotalSeconds:N2}s", TablerIconType.Bolt, TablerColor.Green)
                    .Metric("Failures", report.Coverage.Count(static item => !item.Succeeded).ToString("N0"), TablerIconType.AlertTriangle, TablerColor.Yellow))
                .AddContent(categoryChart)
                .AddContent(eventsCard)
                .AddContent(coverageCard)));
        return document.ToString();
    }

    /// <summary>Writes a self-contained HTML report.</summary>
    public static string Save(EventReport report, string path, bool open = false) {
        if (string.IsNullOrWhiteSpace(path)) {
            throw new ArgumentException("Output path cannot be empty.", nameof(path));
        }
        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory!);
        }
        File.WriteAllText(fullPath, Render(report), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        if (open) {
            Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
        }
        return fullPath;
    }
}

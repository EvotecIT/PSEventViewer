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
        foreach (EventReportSection section in report.Sections
                     .OrderByDescending(static section => section.Rows.Count)
                     .Take(12)) {
            categoryChart.AddItem(section.DisplayName, section.Rows.Count);
        }
        List<object> coverage = report.Coverage.Cast<object>().ToList();
        var coverageCard = new TablerDataTableCard()
            .Title("Coverage")
            .Subtitle("Every queried computer and source channel")
            .Accent(report.Coverage.All(static item => item.Succeeded) ? TablerColor.Green : TablerColor.Yellow)
            .Bind(coverage);
        var workspace = new TablerReportWorkspace()
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
            .Content(content => {
                content.Hero(hero => hero
                    .Title(report.Title)
                    .Chip("Windows Event Log"));
                content.Metrics(metrics => metrics
                    .Metric("Events", report.Rows.Count.ToString("N0"), TablerIconType.ListDetails, TablerColor.Blue)
                    .Metric("Sources", report.Coverage.Count.ToString("N0"), TablerIconType.Server, TablerColor.Indigo)
                    .Metric("Query", $"{report.QueryDuration.TotalSeconds:N2}s", TablerIconType.Bolt, TablerColor.Green)
                    .Metric("Failures", report.Coverage.Count(static item => !item.Succeeded).ToString("N0"), TablerIconType.AlertTriangle, TablerColor.Yellow));
                content.AddContent(categoryChart);
                foreach (EventReportSection section in report.Sections) {
                    List<Dictionary<string, object?>> rows = EventReportTableProjection.Project(section);
                    string subtitle = string.IsNullOrWhiteSpace(section.Description)
                        ? $"{section.Rows.Count:N0} matching event{(section.Rows.Count == 1 ? string.Empty : "s")}"
                        : $"{section.Rows.Count:N0} matching event{(section.Rows.Count == 1 ? string.Empty : "s")} · {section.Description}";
                    content.AddContent(new TablerDataTableCard()
                        .Title(section.DisplayName)
                        .Subtitle(subtitle)
                        .Accent(section.Kind == EventReportSectionKind.Generic ? TablerColor.Blue : TablerColor.Indigo)
                        .Bind(rows.Cast<object>(), table => table.Settings(settings => settings
                            .Searching(true)
                            .Ordering(true)
                            .Paging(25, new[] { 25, 50, 100, -1 })
                            .ResponsiveInline()
                            .End())));
                }
                content.AddContent(coverageCard);
            });
        document.Body.Add(workspace);
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

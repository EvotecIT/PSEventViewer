using OfficeIMO.Data;
using OfficeIMO.Drawing;
using OfficeIMO.Excel;
using OfficeIMO.Excel.Fluent;

namespace EventViewerX.Reporting;

/// <summary>Renders a polished OfficeIMO.Excel workbook from an event report snapshot.</summary>
public static class EventReportExcelRenderer {
    /// <summary>Writes Summary, Events, and Coverage worksheets without re-querying event sources.</summary>
    public static string Save(EventReport report, string path) {
        if (report == null) {
            throw new ArgumentNullException(nameof(report));
        }
        if (string.IsNullOrWhiteSpace(path)) {
            throw new ArgumentException("Output path cannot be empty.", nameof(path));
        }
        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory!);
        }
        using var document = ExcelDocument.Create(fullPath);
        document.AsFluent().Info(info => info
            .Title(report.Title)
            .Author("EventViewerX")
            .Company("Evotec")
            .Application("EventViewerX.Reporting")
            .Keywords("windows,event log,security,report"))
            .End();

        DateTime? firstEvent = report.Rows.Count == 0 ? null : report.Rows.Min(static row => row.TimeCreated);
        DateTime? lastEvent = report.Rows.Count == 0 ? null : report.Rows.Max(static row => row.TimeCreated);

        var summary = new SheetComposer(document, "Summary");
        summary.Title(report.Title, $"Generated {report.GeneratedAt:u}")
            .KpiRow(new (string, object?)[] {
                ("Events", report.Rows.Count),
                ("Sources", report.Coverage.Count),
                ("Failures", report.Coverage.Count(static item => !item.Succeeded)),
                ("Query Seconds", Math.Round(report.QueryDuration.TotalSeconds, 3)),
                ("Candidates", report.EventsScanned),
                ("Limit Reached", report.ScanLimitReached ? "Yes" : "No")
            }, perRow: 3)
            .Section("Report scope")
            .PropertiesGrid(new (string, object?)[] {
                ("First event", firstEvent?.ToString("u") ?? "No matching events"),
                ("Last event", lastEvent?.ToString("u") ?? "No matching events"),
                ("Healthy sources", report.Coverage.Count(static item => item.Succeeded)),
                ("Failed sources", report.Coverage.Count(static item => !item.Succeeded))
            }, columns: 2);
        List<Dictionary<string, object?>> typeRows = report.Rows
                .GroupBy(static row => row.Type, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(static group => group.Count())
                .ThenBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .Select(static group => new Dictionary<string, object?> {
                    ["Type"] = group.Key,
                    ["Events"] = group.Count()
                })
                .ToList();
        string typesRange = summary.TableFrom(typeRows, "Top event types",
            configure: options => {
                options.HeaderCase = HeaderCase.Title;
                options.Columns = new[] { "Type", "Events" };
            },
            style: ExcelTableStyle.TableStyleLight9,
            visuals: visuals => {
                visuals.NumericColumnFormats["Events"] = "#,##0";
                visuals.DataBars["Events"] = OfficeColor.ParseHex("#2563EB");
            });
        summary.ApplyColumnSizing(typesRange, options => {
            options.WidthByHeader["Type"] = 32;
            options.WidthByHeader["Events"] = 24;
        });
        summary.Sheet.SetColumnWidth(3, 20);
        summary.Sheet.SetColumnWidth(4, 24);
        if (typeRows.Count > 0) {
            summary.Sheet.AddTopNBarChart(typesRange, row: 10, column: 6,
                    title: "Most frequent event types", widthPixels: 600, heightPixels: 330)
                .HideLegend()
                .SetTitleTextStyle(fontSizePoints: 12, bold: true, color: "1F2937")
                .SetCategoryAxisLabelTextStyle(fontSizePoints: 8, color: "374151")
                .SetValueAxisGridlines(showMajor: true, showMinor: false, lineColor: "E5E7EB", lineWidthPoints: 0.5)
                .SetValueAxisNumberFormat("#,##0", sourceLinked: false);
        }
        summary.PrintDefaults(showGridlines: false).Finish(autoFitColumns: false);

        var events = new SheetComposer(document, "Events");
        events.Title("Events", "Normalized common fields followed by type-specific properties");
        List<Dictionary<string, object?>> eventRows = EventReportTableProjection.Project(report.Rows);
        string[] eventColumns = BuildEventColumns(eventRows);
        bool hasCollapsedDetails = eventColumns.Contains("Details", StringComparer.OrdinalIgnoreCase);
        string eventsRange = events.TableFrom(eventRows, "Events",
            configure: options => {
                options.HeaderCase = HeaderCase.Title;
                options.NullPolicy = NullPolicy.EmptyString;
                options.Columns = eventColumns;
            },
            style: ExcelTableStyle.TableStyleLight9,
            visuals: visuals => {
                visuals.NumericColumnFormats["Time Created"] = "yyyy-mm-dd hh:mm:ss";
                visuals.NumericColumnFormats["Event ID"] = "0";
                visuals.NumericColumnFormats["Record ID"] = "0";
                visuals.AutoFormatDynamicCollections = false;
                visuals.TextBackgrounds["Level"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                    ["Critical"] = "#FECACA",
                    ["Error"] = "#FEE2E2",
                    ["Warning"] = "#FEF3C7",
                    ["Information"] = "#DBEAFE",
                    ["Verbose"] = "#EDE9FE"
                };
                visuals.BoldByText["Level"] = new HashSet<string>(new[] { "Critical", "Error" }, StringComparer.OrdinalIgnoreCase);
            });
        events.ApplyColumnSizing(eventsRange, options => {
            options.MediumWidth = 20;
            options.WidthByHeader["Time Created"] = 20;
            options.WidthByHeader["Type"] = 28;
            options.WidthByHeader["Event ID"] = 10;
            options.WidthByHeader["Level"] = 13;
            options.WidthByHeader["Source Computer"] = 22;
            options.WidthByHeader["Source Log"] = 24;
            options.WidthByHeader["Provider"] = 26;
            options.WidthByHeader["Record ID"] = 13;
            options.WidthByHeader["Details"] = 42;
            options.WidthByHeader["Message"] = 52;
            options.WidthByHeader["Container Log"] = 38;
            options.WidthByHeader["Collector Computer"] = 28;
            options.WrapHeaders.UnionWith(new[] { "Container Log", "Collector Computer" });
            if (!hasCollapsedDetails) {
                options.WrapHeaders.Add("Message");
            }
        });
        (int eventsHeaderRow, int eventsLastRow) = GetRowBounds(eventsRange);
        for (int row = eventsHeaderRow + 1; row <= eventsLastRow; row++) {
            events.Sheet.SetRowHeight(row, hasCollapsedDetails ? 24 : 45);
        }
        events.PrintDefaults(showGridlines: false, fitToWidth: 1).Finish(autoFitColumns: false);

        var coverage = new SheetComposer(document, "Coverage");
        coverage.Title("Coverage", "Queried source matrix and isolated failures");
        string coverageRange = coverage.TableFrom(report.Coverage, "Coverage",
            configure: options => {
                options.HeaderCase = HeaderCase.Title;
                options.Columns = new[] { "Succeeded", "Status", "MachineName", "LogName", "Detail" };
            },
            style: ExcelTableStyle.TableStyleLight9,
            visuals: visuals => {
                visuals.TextBackgrounds["Status"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                    ["Succeeded"] = "#DCFCE7",
                    ["Supplied"] = "#DBEAFE",
                    ["AccessDenied"] = "#FEE2E2",
                    ["Timeout"] = "#FEF3C7",
                    ["HostUnavailable"] = "#FEE2E2",
                    ["EventLogError"] = "#FEE2E2"
                };
                visuals.BoldByText["Status"] = new HashSet<string>(new[] {
                    "AccessDenied", "Timeout", "HostUnavailable", "EventLogError"
                }, StringComparer.OrdinalIgnoreCase);
            });
        coverage.ApplyColumnSizing(coverageRange, options => {
            options.WidthByHeader["Succeeded"] = 12;
            options.WidthByHeader["Status"] = 18;
            options.WidthByHeader["Machine Name"] = 24;
            options.WidthByHeader["Log Name"] = 42;
            options.WidthByHeader["Detail"] = 60;
            options.WrapHeaders.UnionWith(new[] { "Log Name", "Detail" });
        }).PrintDefaults(showGridlines: false, fitToWidth: 1).Finish(autoFitColumns: false);
        document.Save();
        return fullPath;
    }

    private static string[] BuildEventColumns(IReadOnlyList<Dictionary<string, object?>> rows) {
        string[] first = {
            nameof(EventReportRow.TimeCreated), nameof(EventReportRow.Type), nameof(EventReportRow.EventId),
            nameof(EventReportRow.Level), nameof(EventReportRow.SourceComputer), nameof(EventReportRow.SourceLog),
            nameof(EventReportRow.Provider), nameof(EventReportRow.RecordId)
        };
        string[] last = {
            "Details", nameof(EventReportRow.Message), nameof(EventReportRow.ContainerLog),
            nameof(EventReportRow.CollectorComputer)
        };
        var available = new HashSet<string>(rows.SelectMany(static row => row.Keys), StringComparer.OrdinalIgnoreCase);
        var reserved = new HashSet<string>(first.Concat(last), StringComparer.OrdinalIgnoreCase);
        return first.Where(available.Contains)
            .Concat(available.Where(column => !reserved.Contains(column)).OrderBy(static column => column, StringComparer.OrdinalIgnoreCase))
            .Concat(last.Where(available.Contains))
            .ToArray();
    }

    private static (int HeaderRow, int LastRow) GetRowBounds(string range) {
        string[] cells = range.Split(':');
        if (cells.Length != 2 || !TryGetRow(cells[0], out int first) || !TryGetRow(cells[1], out int last)) {
            throw new InvalidDataException($"OfficeIMO returned an invalid table range '{range}'.");
        }
        return (first, last);
    }

    private static bool TryGetRow(string cell, out int row) {
        row = 0;
        int index = 0;
        while (index < cell.Length && !char.IsDigit(cell[index])) {
            index++;
        }
        return index < cell.Length && int.TryParse(cell.Substring(index), out row);
    }
}

using HtmlForgeX.Email;

namespace EventViewerX.Reporting;

/// <summary>Creates responsive event-report email payloads without choosing a mail transport.</summary>
public static class EventReportEmailRenderer {
    /// <summary>Renders a compact email digest for Mailozaurr or another transport.</summary>
    public static async Task<EventEmailPackage> RenderAsync(EventReport report, int maximumRows = 25) {
        if (report == null) {
            throw new ArgumentNullException(nameof(report));
        }
        if (maximumRows < 0) {
            throw new ArgumentOutOfRangeException(nameof(maximumRows));
        }
        string subject = $"{report.Title} - {report.Rows.Count:N0} events";
        var email = new Email()
            .WithThemeMode(EmailThemeMode.Auto)
            .ConfigureLayout(containerPadding: "16px", contentPadding: "12px", maxWidth: "760px");
        email.Head.AddTitle(report.Title);
        email.Body.PreheaderText = $"{report.Rows.Count:N0} events across {report.Coverage.Count:N0} sources.";
        email.Body.EmailBox(box => {
            box.EmailHero(hero => hero
                .WithIcon("🛡️")
                .WithEyebrow("EventViewerX")
                .WithTitle(report.Title)
                .WithSubtitle($"{report.Rows.Count:N0} events · {report.QueryDuration.TotalSeconds:N2}s query"));
            box.EmailDivider().WithPattern(EmailDividerPattern.Solid);
            var metrics = new EmailGrid().WithColumns(3).WithGap(GridGap.Small).WithStackOnMobile(true);
            metrics.Add(new EmailMetricTile().WithIcon("📋").WithValue(report.Rows.Count.ToString("N0"), "Events"));
            metrics.Add(new EmailMetricTile().WithIcon("🖥️").WithValue(report.Coverage.Count.ToString("N0"), "Sources"));
            metrics.Add(new EmailMetricTile().WithIcon("⚠️").WithValue(report.Coverage.Count(static item => !item.Succeeded).ToString("N0"), "Failures"));
            box.Add(metrics);
            List<Dictionary<string, object?>> rows = report.Rows.Take(maximumRows)
                .Select(static row => {
                    var values = new Dictionary<string, object?> {
                        ["Time"] = row.TimeCreated,
                        ["Type"] = row.Type,
                        ["Event ID"] = row.EventId,
                        ["Computer"] = row.SourceComputer
                    };
                    string details = EventReportTableProjection.FormatDetails(row, maximumValues: 5);
                    if (!string.IsNullOrWhiteSpace(details)) {
                        values["Details"] = details;
                    }
                    values["Message"] = row.Message;
                    return values;
                }).ToList();
            if (rows.Count > 0) {
                box.EmailDivider().WithPattern(EmailDividerPattern.Dashed);
                box.EmailTable(rows);
            }
        });
        EmailRenderResult result = await email.RenderAsync().ConfigureAwait(false);
        string plainText = $"{report.Title}{Environment.NewLine}{report.Rows.Count:N0} events across {report.Coverage.Count:N0} sources.{Environment.NewLine}Query duration: {report.QueryDuration.TotalSeconds:N2}s";
        return new EventEmailPackage(subject, plainText, result);
    }
}

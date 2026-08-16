using System.Globalization;

namespace EventViewerX.Reporting;

/// <summary>Builds a readable tabular projection without losing heterogeneous provider payloads.</summary>
internal static class EventReportTableProjection {
    internal const int DefaultExpandedValueColumnLimit = 12;

    internal static List<Dictionary<string, object?>> Project(
        IEnumerable<EventReportRow> source,
        int expandedValueColumnLimit = DefaultExpandedValueColumnLimit) {
        if (source == null) {
            throw new ArgumentNullException(nameof(source));
        }
        if (expandedValueColumnLimit < 0) {
            throw new ArgumentOutOfRangeException(nameof(expandedValueColumnLimit));
        }
        List<EventReportRow> rows = source.ToList();
        int valueColumnCount = rows
            .SelectMany(static row => row.Values.Keys)
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(expandedValueColumnLimit + 1)
            .Count();
        if (valueColumnCount <= expandedValueColumnLimit) {
            return rows.Select(static row => row.ToDictionary()
                    .ToDictionary(static item => item.Key, static item => item.Value, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
        return rows.Select(static row => CreateCollapsedRow(row)).ToList();
    }

    internal static string FormatDetails(
        EventReportRow row,
        int maximumValues = int.MaxValue,
        string? separator = null) {
        if (row == null) {
            throw new ArgumentNullException(nameof(row));
        }
        if (maximumValues < 0) {
            throw new ArgumentOutOfRangeException(nameof(maximumValues));
        }
        return string.Join(separator ?? Environment.NewLine, row.Values
            .Where(static item => !string.IsNullOrWhiteSpace(item.Key))
            .OrderBy(static item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Take(maximumValues)
            .Select(static item => $"{item.Key}: {FormatValue(item.Value)}"));
    }

    private static Dictionary<string, object?> CreateCollapsedRow(EventReportRow row) => new(StringComparer.OrdinalIgnoreCase) {
        [nameof(EventReportRow.TimeCreated)] = row.TimeCreated,
        [nameof(EventReportRow.Type)] = row.Type,
        [nameof(EventReportRow.EventId)] = row.EventId,
        [nameof(EventReportRow.Level)] = row.Level,
        [nameof(EventReportRow.SourceComputer)] = row.SourceComputer,
        [nameof(EventReportRow.SourceLog)] = row.SourceLog,
        [nameof(EventReportRow.Provider)] = row.Provider,
        [nameof(EventReportRow.RecordId)] = row.RecordId,
        ["Details"] = FormatDetails(row, separator: "; "),
        [nameof(EventReportRow.Message)] = CollapseWhitespace(row.Message),
        [nameof(EventReportRow.ContainerLog)] = row.ContainerLog,
        [nameof(EventReportRow.CollectorComputer)] = row.CollectorComputer
    };

    private static string FormatValue(object? value) => value switch {
        null => string.Empty,
        DateTime date => date.ToString("u", CultureInfo.InvariantCulture),
        DateTimeOffset date => date.ToString("u", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };

    private static string CollapseWhitespace(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return string.Empty;
        }
        var result = new StringBuilder(value!.Length);
        bool pendingSpace = false;
        foreach (char character in value) {
            if (char.IsWhiteSpace(character)) {
                pendingSpace = result.Length > 0;
            } else {
                if (pendingSpace) {
                    result.Append(' ');
                    pendingSpace = false;
                }
                result.Append(character);
            }
        }
        return result.ToString();
    }
}

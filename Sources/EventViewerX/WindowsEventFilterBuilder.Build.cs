using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace EventViewerX;

/// <summary>
/// Builds Windows Event Log XPath and QueryList filters from strongly validated values.
/// </summary>
public static partial class WindowsEventFilterBuilder {
    /// <summary>
    /// Builds an Event Viewer / Get-WinEvent compatible XPath query (or XML QueryList) from high-level filters.
    /// </summary>
    /// <param name="id">Event IDs to include.</param>
    /// <param name="eventRecordId">Specific record IDs to include.</param>
    /// <param name="startTime">Earliest timestamp to include as an absolute UTC boundary.</param>
    /// <param name="endTime">Latest timestamp to include as an absolute UTC boundary.</param>
    /// <param name="data">EventData string values to match.</param>
    /// <param name="providerName">Provider names to match.</param>
    /// <param name="keywords">Keyword bitmasks to match.</param>
    /// <param name="level">Trace/Event levels to include (e.g., Error, Warning).</param>
    /// <param name="userId">SIDs or accounts to match in Security context.</param>
    /// <param name="namedDataFilter">Hashtable filters for EventData[@Name] equality.</param>
    /// <param name="namedDataExcludeFilter">Hashtable filters for EventData[@Name] inequality.</param>
    /// <param name="excludeId">Event IDs to exclude.</param>
    /// <param name="logName">Log name (used when emitting QueryList XML).</param>
    /// <param name="path">Optional EVTX file path; when set, QueryList uses file://.</param>
    /// <param name="xpathOnly">When true, returns raw XPath instead of QueryList XML.</param>
    /// <param name="minimumEventRecordIdExclusive">Optional native lower bound used by checkpoint/resume queries.</param>
    /// <param name="maximumEventRecordIdExclusive">Optional native upper bound used by reverse paged queries.</param>
    /// <returns>XPath fragment or full QueryList XML depending on <paramref name="xpathOnly"/> and <paramref name="path"/>.</returns>
    public static string BuildWinEventFilter(
        string[]? id = null,
        string[]? eventRecordId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        string[]? data = null,
        string[]? providerName = null,
        long[]? keywords = null,
        string[]? level = null,
        string[]? userId = null,
        Hashtable[]? namedDataFilter = null,
        Hashtable[]? namedDataExcludeFilter = null,
        string[]? excludeId = null,
        string? logName = null,
        string? path = null,
        bool xpathOnly = false,
        long? minimumEventRecordIdExclusive = null,
        long? maximumEventRecordIdExclusive = null) {
        if (minimumEventRecordIdExclusive < 0) {
            throw new ArgumentOutOfRangeException(nameof(minimumEventRecordIdExclusive), "Minimum event record ID must be greater than or equal to zero.");
        }
        if (maximumEventRecordIdExclusive < 0) {
            throw new ArgumentOutOfRangeException(nameof(maximumEventRecordIdExclusive), "Maximum event record ID must be greater than or equal to zero.");
        }

        int expressionCount = CountWinEventFilterExpressions(
            id,
            eventRecordId,
            startTime,
            endTime,
            data,
            providerName,
            keywords,
            level,
            userId,
            namedDataFilter,
            namedDataExcludeFilter,
            excludeId,
            minimumEventRecordIdExclusive,
            maximumEventRecordIdExclusive);
        if (expressionCount >
            EventFilterCompiler.MaximumXPathExpressions) {
            throw new ArgumentException(
                $"The filter contains {expressionCount} XPath expressions; Windows Event Log supports at most {EventFilterCompiler.MaximumXPathExpressions}. Split the query or reduce the filter values.");
        }

        var filter = string.Empty;
        if (id != null && id.Length > 0) {
            string[] validIds = ValidatePositiveNumericValues(id, int.MaxValue, nameof(id));
            filter = JoinXPathFilter(InitializeXPathFilter(validIds, "EventID={0}", "*[System[{0}]]"), filter);
        }
        if (eventRecordId != null && eventRecordId.Length > 0) {
            string[] validRecordIds = ValidatePositiveNumericValues(eventRecordId, long.MaxValue, nameof(eventRecordId));
            filter = JoinXPathFilter(InitializeXPathFilter(validRecordIds, "EventRecordID={0}", "*[System[{0}]]"), filter);
        }
        if (minimumEventRecordIdExclusive.HasValue) {
            filter = JoinXPathFilter(
                $"*[System[EventRecordID>{minimumEventRecordIdExclusive.Value.ToString(CultureInfo.InvariantCulture)}]]",
                filter);
        }
        if (maximumEventRecordIdExclusive.HasValue) {
            filter = JoinXPathFilter(
                $"*[System[EventRecordID<{maximumEventRecordIdExclusive.Value.ToString(CultureInfo.InvariantCulture)}]]",
                filter);
        }
        if (excludeId != null && excludeId.Length > 0) {
            string[] validExcludedIds = ValidatePositiveNumericValues(excludeId, int.MaxValue, nameof(excludeId));
            filter = JoinXPathFilter(InitializeXPathFilter(validExcludedIds, "EventID!={0}", "*[System[{0}]]", logic: "and"), filter);
        }

        if (startTime.HasValue) {
            filter = JoinXPathFilter($"*[System[TimeCreated[@SystemTime>='{FormatEventTimeUtc(startTime.Value)}']]]", filter);
        }
        if (endTime.HasValue) {
            filter = JoinXPathFilter($"*[System[TimeCreated[@SystemTime<='{FormatEventTimeUtc(endTime.Value)}']]]", filter);
        }
        if (data != null && data.Length > 0) {
            filter = JoinXPathFilter(InitializeXPathFilter(data, "Data={0}", "*[EventData[{0}]]", formatStringLiterals: true, parameterName: nameof(data)), filter);
        }
        if (providerName != null && providerName.Length > 0) {
            filter = JoinXPathFilter(InitializeXPathFilter(providerName, "@Name={0}", "*[System[Provider[{0}]]]", formatStringLiterals: true, parameterName: nameof(providerName)), filter);
        }
        if (level != null && level.Length > 0) {
            var levels = level.Select(l => ((int)Enum.Parse(typeof(System.Diagnostics.Tracing.EventLevel), l)).ToString());
            filter = JoinXPathFilter(InitializeXPathFilter(levels, "Level={0}", "*[System[{0}]]"), filter);
        }
        if (keywords != null && keywords.Length > 0) {
            long keywordFilter = 0;
            foreach (var k in keywords) {
                keywordFilter = keywordFilter == 0 ? k : keywordFilter | k;
            }
            filter = JoinXPathFilter($"*[System[band(Keywords,{keywordFilter})]]", filter);
        }
        if (userId != null && userId.Length > 0) {
            var sids = new List<string>();
            foreach (var item in userId) {
                if (!userSidCache.TryGetValue(item, out var sidString)) {
                    if (!EventStructuredQueryFilterService.TryResolveUserId(item, out sidString)) {
                        throw new ArgumentException($"User identifier '{item}' is not a valid SID or resolvable account name.", nameof(userId));
                    }
                    userSidCache[item] = sidString!;
                }
                sids.Add(sidString!);
            }
            filter = JoinXPathFilter(InitializeXPathFilter(sids, "@UserID={0}", "*[System[Security[{0}]]]", formatStringLiterals: true, parameterName: nameof(userId)), filter);
        }
        if (namedDataFilter != null && namedDataFilter.Length > 0) {
            var items = new List<string>();
            foreach (Hashtable table in namedDataFilter) {
                var keyFilters = new List<string>();
                foreach (var key in table.Keys) {
                    var keyName = FormatXPathStringLiteral(key?.ToString() ?? string.Empty, nameof(namedDataFilter));
                    var values = AsEnumerable(table[key!]);
                    if (values.Any()) {
                        keyFilters.Add(InitializeXPathFilter(values, $"Data[@Name={keyName}] = {{0}}", "{0}", "or", true, formatStringLiterals: true, parameterName: nameof(namedDataFilter)));
                    } else {
                        keyFilters.Add($"Data[@Name={keyName}]");
                    }
                }
                items.Add(InitializeXPathFilter(keyFilters, "{0}", "{0}"));
            }
            filter = JoinXPathFilter(InitializeXPathFilter(items, "{0}", "*[EventData[{0}]]"), filter);
        }
        if (namedDataExcludeFilter != null && namedDataExcludeFilter.Length > 0) {
            var items = new List<string>();
            foreach (Hashtable table in namedDataExcludeFilter) {
                var keyFilters = new List<string>();
                foreach (var key in table.Keys) {
                    var keyName = FormatXPathStringLiteral(key?.ToString() ?? string.Empty, nameof(namedDataExcludeFilter));
                    var values = AsEnumerable(table[key!]);
                    if (values.Any()) {
                        keyFilters.Add(InitializeXPathFilter(values, $"Data[@Name={keyName}] != {{0}}", "{0}", "and", true, formatStringLiterals: true, parameterName: nameof(namedDataExcludeFilter)));
                    } else {
                        keyFilters.Add($"Data[@Name={keyName}]");
                    }
                }
                items.Add(InitializeXPathFilter(keyFilters, "{0}", "{0}"));
            }
            filter = JoinXPathFilter(InitializeXPathFilter(items, "{0}", "*[EventData[{0}]]"), filter);
        }

        if (!xpathOnly && !string.IsNullOrEmpty(filter)) {
            filter = filter.Replace(" and ", " and\n").Replace(" or ", " or\n");
        }

        if (xpathOnly) {
            if (string.IsNullOrWhiteSpace(filter)) {
                filter = "*";
            }
            return filter;
        }

        if (string.IsNullOrWhiteSpace(filter)) {
            filter = "*";
        }

        if (!string.IsNullOrEmpty(path)) {
            var selectFilter = EscapeXmlValue(string.IsNullOrEmpty(filter) ? "*" : filter);
            var escapedPath = EscapeXmlValue(path!);
            return $"<QueryList><Query Id=\"0\" Path=\"file://{escapedPath}\"><Select>{selectFilter}</Select></Query></QueryList>";
        }
        var escapedLog = EscapeXmlValue(logName ?? string.Empty);
        var escapedFilter = EscapeXmlValue(filter);
        return $"<QueryList><Query Id=\"0\" Path=\"{escapedLog}\"><Select Path=\"{escapedLog}\">{escapedFilter}</Select></Query></QueryList>";
    }

    internal static int CountWinEventFilterExpressions(
        string[]? id,
        string[]? eventRecordId,
        DateTime? startTime,
        DateTime? endTime,
        string[]? data,
        string[]? providerName,
        long[]? keywords,
        string[]? level,
        string[]? userId,
        Hashtable[]? namedDataFilter,
        Hashtable[]? namedDataExcludeFilter,
        string[]? excludeId,
        long? minimumEventRecordIdExclusive,
        long? maximumEventRecordIdExclusive) {

        int count = id?.Length ?? 0;
        count += eventRecordId?.Length ?? 0;
        count += excludeId?.Length ?? 0;
        count += startTime.HasValue ? 1 : 0;
        count += endTime.HasValue ? 1 : 0;
        count += data?.Length ?? 0;
        count += providerName?.Length ?? 0;
        count += keywords?.Length > 0 ? 1 : 0;
        count += level?.Length ?? 0;
        count += userId?.Length ?? 0;
        count += CountNamedDataExpressions(namedDataFilter);
        count += CountNamedDataExpressions(namedDataExcludeFilter);
        count += minimumEventRecordIdExclusive.HasValue ? 1 : 0;
        count += maximumEventRecordIdExclusive.HasValue ? 1 : 0;
        return count;
    }

    internal static int CountNamedDataExpressions(Hashtable[]? filters) {
        int count = 0;
        if (filters == null) {
            return count;
        }

        foreach (Hashtable table in filters) {
            foreach (object? key in table.Keys) {
                List<string> values = AsEnumerable(table[key!]).ToList();
                // A valued named-data predicate contains both the @Name comparison and the value comparison.
                count += values.Count > 0 ? values.Count * 2 : 1;
            }
        }
        return count;
    }

    private static string[] ValidatePositiveNumericValues(string[] values, long maximum, string parameterName) {
        var normalized = new string[values.Length];
        for (int index = 0; index < values.Length; index++) {
            string? value = values[index];
            if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long parsed) || parsed <= 0 || parsed > maximum) {
                throw new ArgumentException($"All {parameterName} values must be positive integers no greater than {maximum}.", parameterName);
            }

            normalized[index] = parsed.ToString(CultureInfo.InvariantCulture);
        }

        return normalized;
    }
}

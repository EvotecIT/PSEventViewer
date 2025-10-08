using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;

namespace EventViewerX;

public partial class SearchEvents {
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
        bool xpathOnly = false) {
        var filter = string.Empty;
        if (id != null && id.Length > 0) {
            filter = JoinXPathFilter(InitializeXPathFilter(id, "EventID={0}", "*[System[{0}]]"), filter);
        }
        if (eventRecordId != null && eventRecordId.Length > 0) {
            filter = JoinXPathFilter(InitializeXPathFilter(eventRecordId, "EventRecordID={0}", "*[System[{0}]]"), filter);
        }
        if (excludeId != null && excludeId.Length > 0) {
            filter = JoinXPathFilter(InitializeXPathFilter(excludeId, "EventID!={0}", "*[System[{0}]]"), filter);
        }

        var now = DateTime.Now;
        if (startTime.HasValue) {
            var diff = Math.Round(now.Subtract(startTime.Value).TotalMilliseconds);
            if (diff < 0) {
                diff = 0;
            }
            filter = JoinXPathFilter($"*[System[TimeCreated[timediff(@SystemTime) &lt;= {diff}]]]", filter);
        }
        if (endTime.HasValue) {
            var diff = Math.Round(now.Subtract(endTime.Value).TotalMilliseconds);
            if (diff < 0) {
                diff = 0;
            }
            filter = JoinXPathFilter($"*[System[TimeCreated[timediff(@SystemTime) &gt;= {diff}]]]", filter);
        }
        if (data != null && data.Length > 0) {
            filter = JoinXPathFilter(InitializeXPathFilter(data, "Data='{0}'", "*[EventData[{0}]]"), filter);
        }
        if (providerName != null && providerName.Length > 0) {
            filter = JoinXPathFilter(InitializeXPathFilter(providerName, "@Name='{0}'", "*[System[Provider[{0}]]]"), filter);
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
                    try {
                        var sid = new System.Security.Principal.SecurityIdentifier(item);
                        sidString = sid.Translate(typeof(System.Security.Principal.SecurityIdentifier)).ToString();
                    } catch {
                        var user = new System.Security.Principal.NTAccount(item);
                        sidString = user.Translate(typeof(System.Security.Principal.SecurityIdentifier)).ToString();
                    }
                    userSidCache[item] = sidString;
                }
                sids.Add(sidString);
            }
            filter = JoinXPathFilter(InitializeXPathFilter(sids, "@UserID='{0}'", "*[System[Security[{0}]]]"), filter);
        }
        if (namedDataFilter != null && namedDataFilter.Length > 0) {
            var items = new List<string>();
            foreach (Hashtable table in namedDataFilter) {
                var keyFilters = new List<string>();
                foreach (var key in table.Keys) {
                    var keyName = EscapeXPathValue(key?.ToString() ?? string.Empty);
                    var values = AsEnumerable(table[key!]);
                    if (values.Any()) {
                        keyFilters.Add(InitializeXPathFilter(values, $"Data[@Name='{keyName}'] = '{{0}}'", "{0}", "or", true));
                    } else {
                        keyFilters.Add($"Data[@Name='{keyName}']");
                    }
                }
                items.Add(InitializeXPathFilter(keyFilters, "{0}", "{0}", escapeItems: false));
            }
            filter = JoinXPathFilter(InitializeXPathFilter(items, "{0}", "*[EventData[{0}]]", escapeItems: false), filter);
        }
        if (namedDataExcludeFilter != null && namedDataExcludeFilter.Length > 0) {
            var items = new List<string>();
            foreach (Hashtable table in namedDataExcludeFilter) {
                var keyFilters = new List<string>();
                foreach (var key in table.Keys) {
                    var keyName = EscapeXPathValue(key?.ToString() ?? string.Empty);
                    var values = AsEnumerable(table[key!]);
                    if (values.Any()) {
                        keyFilters.Add(InitializeXPathFilter(values, $"Data[@Name='{keyName}'] != '{{0}}'", "{0}", "and", true));
                    } else {
                        keyFilters.Add($"Data[@Name='{keyName}']");
                    }
                }
                items.Add(InitializeXPathFilter(keyFilters, "{0}", "{0}", escapeItems: false));
            }
            filter = JoinXPathFilter(InitializeXPathFilter(items, "{0}", "*[EventData[{0}]]", escapeItems: false), filter);
        }

        if (!xpathOnly && !string.IsNullOrEmpty(filter)) {
            filter = filter.Replace(" and ", " and\n").Replace(" or ", " or\n");
        }

        if (xpathOnly) {
            return filter;
        }

        if (!string.IsNullOrEmpty(path)) {
            var selectFilter = string.IsNullOrEmpty(filter) ? "*" : filter;
            var escapedPath = EscapeXPathValue(path!);
            return $"<QueryList><Query Id=\"0\" Path=\"file://{escapedPath}\"><Select>{selectFilter}</Select></Query></QueryList>";
        }
        var escapedLog = EscapeXPathValue(logName ?? string.Empty);
        return $"<QueryList><Query Id=\"0\" Path=\"{escapedLog}\"><Select Path=\"{escapedLog}\">{filter}</Select></Query></QueryList>";
    }
}

namespace EventViewerX;

public partial class SearchEvents {
    private static string EscapeXPathValue(string value) {
        return System.Security.SecurityElement.Escape(value);
    }
    private static string JoinXPathFilter(string newFilter, string existingFilter = "", string logic = "and", bool noParenthesis = false) {
        if (!string.IsNullOrEmpty(existingFilter)) {
            return noParenthesis
                ? $"{existingFilter} {logic} {newFilter}"
                : $"({existingFilter}) {logic} ({newFilter})";
        }
        return newFilter;
    }

    private static string InitializeXPathFilter(IEnumerable<object> items, string forEachFormatString, string finalizeFormatString, string logic = "or", bool noParenthesis = false, bool escapeItems = true) {
        var filter = string.Empty;
        foreach (var item in items) {
            var value = escapeItems ? EscapeXPathValue(item.ToString()) : item.ToString();
            var formatted = string.Format(forEachFormatString, value);
            filter = JoinXPathFilter(formatted, filter, logic, noParenthesis);
        }
        return string.Format(finalizeFormatString, filter);
    }

    private static IEnumerable<string> AsEnumerable(object obj) {
        if (obj is IEnumerable enumerable and not string) {
            foreach (var o in enumerable) {
                if (o != null) {
                    yield return o.ToString();
                }
            }
        } else if (obj != null) {
            yield return obj.ToString();
        }
    }

    /// <summary>
    /// Cache for translated user identifiers to avoid repeated lookups
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> userSidCache = new ConcurrentDictionary<string, string>();

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
            filter = JoinXPathFilter($"*[System[TimeCreated[timediff(@SystemTime) &lt;= {diff}]]]", filter);
        }
        if (endTime.HasValue) {
            var diff = Math.Round(now.Subtract(endTime.Value).TotalMilliseconds);
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
                    var keyName = EscapeXPathValue(key.ToString());
                    var values = AsEnumerable(table[key]);
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
                    var keyName = EscapeXPathValue(key.ToString());
                    var values = AsEnumerable(table[key]);
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

        if (xpathOnly) {
            return filter;
        }

        if (!string.IsNullOrEmpty(path)) {
            var selectFilter = string.IsNullOrEmpty(filter) ? "*" : filter;
            var escapedPath = EscapeXPathValue(path);
            return $"<QueryList><Query Id=\"0\" Path=\"file://{escapedPath}\"><Select>{selectFilter}</Select></Query></QueryList>";
        }
        var escapedLog = EscapeXPathValue(logName ?? string.Empty);
        return $"<QueryList><Query Id=\"0\" Path=\"{escapedLog}\"><Select Path=\"{escapedLog}\">{filter}</Select></Query></QueryList>";
    }
}
namespace PSEventViewer;

[Cmdlet(VerbsCommon.Get, "WinEventFilter")]
[OutputType(typeof(string))]
public sealed class CmdletGetWinEventFilter : AsyncPSCmdlet {
    [Parameter]
    public string[] ID;

    [Alias("RecordID")]
    [Parameter]
    public string[] EventRecordID;

    [Parameter]
    public DateTime? StartTime;

    [Parameter]
    public DateTime? EndTime;

    [Parameter]
    public string[] Data;

    [Parameter]
    public string[] ProviderName;

    [Parameter]
    public long[] Keywords;

    [ValidateSet("Critical","Error","Informational","LogAlways","Verbose","Warning")]
    [Parameter]
    public string[] Level;

    [Parameter]
    public string[] UserID;

    [Parameter]
    public Hashtable[] NamedDataFilter;

    [Parameter]
    public Hashtable[] NamedDataExcludeFilter;

    [Parameter]
    public string[] ExcludeID;

    [Parameter]
    public string LogName;

    [Parameter]
    public string Path;

    [Parameter]
    public SwitchParameter XPathOnly;

    private static string JoinXPathFilter(string newFilter, string existingFilter = "", string logic = "and", bool noParenthesis = false) {
        if (!string.IsNullOrEmpty(existingFilter)) {
            if (noParenthesis) {
                return $"{existingFilter} {logic} {newFilter}";
            } else {
                return $"({existingFilter}) {logic} ({newFilter})";
            }
        } else {
            return newFilter;
        }
    }

    private static string InitializeXPathFilter(IEnumerable<object> items, string forEachFormatString, string finalizeFormatString, string logic = "or", bool noParenthesis = false) {
        string filter = string.Empty;
        foreach (var item in items) {
            var formatted = string.Format(forEachFormatString, item);
            filter = JoinXPathFilter(formatted, filter, logic, noParenthesis);
        }
        return string.Format(finalizeFormatString, filter);
    }

    private static IEnumerable<string> AsEnumerable(object obj) {
        if (obj is IEnumerable enumerable && obj is not string) {
            foreach (var o in enumerable) {
                yield return o.ToString();
            }
        } else if (obj != null) {
            yield return obj.ToString();
        }
    }

    protected override Task ProcessRecordAsync() {
        var filter = string.Empty;
        if (ID != null && ID.Length > 0) {
            filter = JoinXPathFilter(InitializeXPathFilter(ID, "EventID={0}", "*[System[{0}]]"), filter);
        }
        if (EventRecordID != null && EventRecordID.Length > 0) {
            filter = JoinXPathFilter(InitializeXPathFilter(EventRecordID, "EventRecordID={0}", "*[System[{0}]]"), filter);
        }
        if (ExcludeID != null && ExcludeID.Length > 0) {
            filter = JoinXPathFilter(InitializeXPathFilter(ExcludeID, "EventID!={0}", "*[System[{0}]]"), filter);
        }

        var now = DateTime.Now;
        if (StartTime.HasValue) {
            var diff = Math.Round(now.Subtract(StartTime.Value).TotalMilliseconds);
            filter = JoinXPathFilter($"*[System[TimeCreated[timediff(@SystemTime) &lt;= {diff}]]]", filter);
        }
        if (EndTime.HasValue) {
            var diff = Math.Round(now.Subtract(EndTime.Value).TotalMilliseconds);
            filter = JoinXPathFilter($"*[System[TimeCreated[timediff(@SystemTime) &gt;= {diff}]]]", filter);
        }
        if (Data != null && Data.Length > 0) {
            filter = JoinXPathFilter(InitializeXPathFilter(Data, "Data='{0}'", "*[EventData[{0}]]"), filter);
        }
        if (ProviderName != null && ProviderName.Length > 0) {
            filter = JoinXPathFilter(InitializeXPathFilter(ProviderName, "@Name='{0}'", "*[System[Provider[{0}]]]"), filter);
        }
        if (Level != null && Level.Length > 0) {
            var levels = Level.Select(l => ((int)Enum.Parse(typeof(System.Diagnostics.Tracing.EventLevel), l)).ToString());
            filter = JoinXPathFilter(InitializeXPathFilter(levels, "Level={0}", "*[System[{0}]]"), filter);
        }
        if (Keywords != null && Keywords.Length > 0) {
            long keywordFilter = 0;
            foreach (var k in Keywords) {
                keywordFilter = keywordFilter == 0 ? k : keywordFilter | k;
            }
            filter = JoinXPathFilter($"*[System[band(Keywords,{keywordFilter})]]", filter);
        }
        if (UserID != null && UserID.Length > 0) {
            var sids = new List<string>();
            foreach (var item in UserID) {
                try {
                    var sid = new System.Security.Principal.SecurityIdentifier(item);
                    sids.Add(sid.Translate(typeof(System.Security.Principal.SecurityIdentifier)).ToString());
                } catch (System.Management.Automation.RuntimeException ex) when (ex.ErrorRecord.CategoryInfo.Category == System.Management.Automation.ErrorCategory.InvalidArgument) {
                    var user = new System.Security.Principal.NTAccount(item);
                    sids.Add(user.Translate(typeof(System.Security.Principal.SecurityIdentifier)).ToString());
                }
            }
            filter = JoinXPathFilter(InitializeXPathFilter(sids, "@UserID='{0}'", "*[System[Security[{0}]]]"), filter);
        }
        if (NamedDataFilter != null && NamedDataFilter.Length > 0) {
            var items = new List<string>();
            foreach (Hashtable table in NamedDataFilter) {
                var keyFilters = new List<string>();
                foreach (var key in table.Keys) {
                    var values = AsEnumerable(table[key]);
                    if (values.Any()) {
                        keyFilters.Add(InitializeXPathFilter(values, $"Data[@Name='{key}'] = '{{0}}'", "{0}", "or", true));
                    } else {
                        keyFilters.Add($"Data[@Name='{key}']");
                    }
                }
                items.Add(InitializeXPathFilter(keyFilters, "{0}", "{0}"));
            }
            filter = JoinXPathFilter(InitializeXPathFilter(items, "{0}", "*[EventData[{0}]]"), filter);
        }
        if (NamedDataExcludeFilter != null && NamedDataExcludeFilter.Length > 0) {
            var items = new List<string>();
            foreach (Hashtable table in NamedDataExcludeFilter) {
                var keyFilters = new List<string>();
                foreach (var key in table.Keys) {
                    var values = AsEnumerable(table[key]);
                    if (values.Any()) {
                        keyFilters.Add(InitializeXPathFilter(values, $"Data[@Name='{key}'] != '{{0}}'", "{0}", "and", true));
                    } else {
                        keyFilters.Add($"Data[@Name='{key}']");
                    }
                }
                items.Add(InitializeXPathFilter(keyFilters, "{0}", "{0}"));
            }
            filter = JoinXPathFilter(InitializeXPathFilter(items, "{0}", "*[EventData[{0}]]"), filter);
        }

        string output;
        if (XPathOnly.IsPresent) {
            output = filter;
        } else {
            if (!string.IsNullOrEmpty(Path)) {
                output = $"<QueryList><Query Id=\"0\" Path=\"file://{Path}\"><Select>{filter}</Select></Query></QueryList>";
            } else {
                output = $"<QueryList><Query Id=\"0\" Path=\"{LogName}\"><Select Path=\"{LogName}\">{filter}</Select></Query></QueryList>";
            }
        }
        WriteObject(output);
        return Task.CompletedTask;
    }
}


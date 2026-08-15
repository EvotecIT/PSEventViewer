namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Creates a reusable typed Windows Event Log filter or compiles it to native query text.</para>
/// <para type="description">The default output is EventViewerX.EventFilter for reuse by C# and cmdlets. Use AsXPath, LogName, or Path when native query text is required by Get-WinEvent, Event Viewer, or WEC.</para>
/// </summary>
/// <example>
///   <summary>Create a reusable typed failed-logon filter</summary>
///   <code>$filter = New-EVXFilter -EventId 4625 -TimePeriod LastDay</code>
///   <para>Returns an EventFilter rather than opaque query text.</para>
/// </example>
/// <example>
///   <summary>Create XPath for Get-WinEvent</summary>
///   <code>Get-WinEvent -LogName Security -FilterXPath (New-EVXFilter -EventId 4625 -AsXPath)</code>
///   <para>Compiles the same typed filter to native XPath.</para>
/// </example>
/// <example>
///   <summary>Create QueryList XML for a custom view or WEC</summary>
///   <code>New-EVXFilter -LogName Security -EventId 4625 -NamedDataExcludeFilter @{ TargetUserName = 'svc_legacy' }</code>
///   <para>Returns QueryList XML with native Select and Suppress clauses.</para>
/// </example>
[Cmdlet(VerbsCommon.New, "EVXFilter", DefaultParameterSetName = "Object")]
[Alias("Get-EVXFilter")]
[OutputType(typeof(EventFilter), ParameterSetName = new[] { "Object" })]
[OutputType(typeof(string), ParameterSetName = new[] { "XPath", "ChannelXml", "FileXml" })]
public sealed class CmdletNewEVXFilter : PSCmdlet {
    /// <summary>Event identifiers to include.</summary>
    [Alias("Id")]
    [Parameter]
    public int[]? EventId { get; set; }

    /// <summary>Event record identifiers to include.</summary>
    [Alias("EventRecordId")]
    [Parameter]
    public long[]? RecordId { get; set; }

    /// <summary>Provider names to include.</summary>
    [Parameter]
    public string[]? ProviderName { get; set; }

    /// <summary>Numeric Windows event levels to include.</summary>
    [Parameter]
    public EventViewerX.Level[]? Level { get; set; }

    /// <summary>Windows keyword masks to include.</summary>
    [Parameter]
    public long[]? Keywords { get; set; }

    /// <summary>Absolute beginning of the time range.</summary>
    [Alias("DateFrom")]
    [Parameter]
    public DateTime? StartTime { get; set; }

    /// <summary>Absolute end of the time range.</summary>
    [Alias("DateTo")]
    [Parameter]
    public DateTime? EndTime { get; set; }

    /// <summary>Named relative time range.</summary>
    [Parameter]
    public TimePeriod? TimePeriod { get; set; }

    /// <summary>User security identifiers to include.</summary>
    [Parameter]
    public string[]? UserId { get; set; }

    /// <summary>Unnamed EventData values to include.</summary>
    [Parameter]
    public string[]? Data { get; set; }

    /// <summary>Named EventData values to include.</summary>
    [Parameter]
    public Hashtable? NamedDataFilter { get; set; }

    /// <summary>Named EventData values to suppress.</summary>
    [Parameter]
    public Hashtable? NamedDataExcludeFilter { get; set; }

    /// <summary>Event identifiers to suppress.</summary>
    [Alias("ExcludeId")]
    [Parameter]
    public int[]? ExcludeEventId { get; set; }

    /// <summary>Returns one native XPath expression.</summary>
    [Parameter(Mandatory = true, ParameterSetName = "XPath")]
    public SwitchParameter AsXPath { get; set; }

    /// <summary>Channel used to produce QueryList XML.</summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ChannelXml")]
    public string[] LogName { get; set; } = Array.Empty<string>();

    /// <summary>Offline event-log files used to produce QueryList XML.</summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "FileXml")]
    [Alias("PSPath")]
    public string[] Path { get; set; } = Array.Empty<string>();

    /// <inheritdoc />
    protected override void ProcessRecord() {
        EventFilter filter = PowerShellEventFilterAdapter.CreateFilter(
            EventId,
            RecordId,
            ProviderName,
            Level,
            Keywords,
            StartTime,
            EndTime,
            TimePeriod,
            UserId,
            Data,
            NamedDataFilter,
            NamedDataExcludeFilter,
            ExcludeEventId);
        if (ParameterSetName == "Object") {
            WriteObject(filter);
            return;
        }

        EventFilterCompiler.SplitNamedDataExclusions(
            filter,
            out EventFilter? select,
            out EventFilter? suppression);
        IReadOnlyList<EventFilter> partitions =
            EventFilterPartitioner.Partition(select!);
        IReadOnlyList<EventFilter> suppressions =
            EventFilterPartitioner.PartitionNamedDataSuppression(suppression);
        if (ParameterSetName == "XPath") {
            if (suppressions.Count > 0) {
                throw new PSArgumentException(
                    "Named-data exclusions require QueryList XML. Supply LogName or Path instead of AsXPath.");
            }
            if (partitions.Count != 1) {
                throw new PSArgumentException(
                    "The filter exceeds one native XPath expression. Supply LogName or Path to generate partitioned QueryList XML.");
            }
            WriteObject(EventFilterCompiler.BuildXPath(partitions[0]));
            return;
        }

        string queryXml = ParameterSetName == "ChannelXml"
            ? EventFilterCompiler.BuildChannelUnionQueryXml(
                LogName,
                partitions,
                suppressions)
            : EventFilterCompiler.BuildFileUnionQueryXml(
                Path.Select(static path => System.IO.Path.GetFullPath(path)),
                partitions,
                suppressions);
        WriteObject(queryXml);
    }
}
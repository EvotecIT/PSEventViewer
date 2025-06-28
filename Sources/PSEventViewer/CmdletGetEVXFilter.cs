namespace PSEventViewer;

/// <summary>
/// Generates XPath filters for Windows Event Log queries.
/// Creates filters compatible with Get-WinEvent -FilterXPath and Event Viewer Custom Views.
/// </summary>
[Cmdlet(VerbsCommon.Get, "EVXFilter")]
[Alias("Get-EventViewerXFilter", "Get-WinEventFilter", "Get-EventsFilter")]
[OutputType(typeof(string))]
public sealed class CmdletGetEVXFilter : AsyncPSCmdlet {
    /// <summary>
    /// Event identifiers to include in the filter.
    /// </summary>
    [Parameter]
    public string[] ID;

    /// <summary>
    /// Event record identifiers to include in the filter.
    /// </summary>
    [Alias("RecordID")]
    [Parameter]
    public string[] EventRecordID;

    /// <summary>
    /// Start time for the filter range.
    /// </summary>
    [Parameter]
    public DateTime? StartTime;

    /// <summary>
    /// End time for the filter range.
    /// </summary>
    [Parameter]
    public DateTime? EndTime;

    /// <summary>
    /// Specific event data values to filter on.
    /// </summary>
    [Parameter]
    public string[] Data;

    /// <summary>
    /// Provider names to include in the filter.
    /// </summary>
    [Parameter]
    public string[] ProviderName;

    /// <summary>
    /// Keywords to include in the filter.
    /// </summary>
    [Parameter]
    public long[] Keywords;

    /// <summary>
    /// Event levels to include in the filter.
    /// </summary>
    [ValidateSet("Critical", "Error", "Informational", "LogAlways", "Verbose", "Warning")]
    [Parameter]
    public string[] Level;

    /// <summary>
    /// User identifiers to include in the filter.
    /// </summary>
    [Parameter]
    public string[] UserID;

    /// <summary>
    /// Hashtable specifying named data filters.
    /// </summary>
    [Parameter]
    public Hashtable[] NamedDataFilter;

    /// <summary>
    /// Hashtable specifying named data to exclude from the filter.
    /// </summary>
    [Parameter]
    public Hashtable[] NamedDataExcludeFilter;

    /// <summary>
    /// Event identifiers to exclude from the filter.
    /// </summary>
    [Parameter]
    public string[] ExcludeID;

    /// <summary>
    /// Name of the log associated with the filter.
    /// </summary>
    [Parameter]
    public string LogName;

    /// <summary>
    /// Path of the log file to generate the filter for.
    /// </summary>
    [Parameter]
    public string Path;

    /// <summary>
    /// When set, outputs only the XPath expression without formatting.
    /// </summary>
    [Parameter]
    public SwitchParameter XPathOnly;

    /// <summary>
    /// Builds the XPath filter based on specified parameters.
    /// </summary>
    protected override Task ProcessRecordAsync() {
        var output = SearchEvents.BuildWinEventFilter(
            ID,
            EventRecordID,
            StartTime,
            EndTime,
            Data,
            ProviderName,
            Keywords,
            Level,
            UserID,
            NamedDataFilter,
            NamedDataExcludeFilter,
            ExcludeID,
            LogName,
            Path,
            XPathOnly.IsPresent);
        WriteObject(output);
        return Task.CompletedTask;
    }
}
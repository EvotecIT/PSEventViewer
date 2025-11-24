namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Generates XPath filters for Windows Event Log queries.</para>
/// <para type="description">Produces filter strings compatible with Get-WinEvent -FilterXPath and Event Viewer Custom Views, supporting include/exclude IDs, time windows, providers, users, keywords, levels, and named data.</para>
/// </summary>
/// <example>
///   <summary>Simple ID filter</summary>
///   <code>Get-EVXFilter -ID 4624,4625 -LogName Security</code>
///   <para>Returns an XPath that matches successful and failed logons.</para>
/// </example>
/// <example>
///   <summary>Filter by provider and time</summary>
///   <code>Get-EVXFilter -ProviderName "Microsoft-Windows-Security-Auditing" -StartTime (Get-Date).AddHours(-4)</code>
///   <para>Limits results to the auditing provider over the last four hours.</para>
/// </example>
/// <example>
///   <summary>Named data include/exclude</summary>
///   <code>Get-EVXFilter -NamedDataFilter @{ TargetUserName='alice' } -ExcludeID 4723</code>
///   <para>Matches events where TargetUserName is alice while excluding password-change attempts.</para>
/// </example>
/// <example>
///   <summary>Output raw XPath only</summary>
///   <code>Get-EVXFilter -ID 1102 -LogName Security -XPathOnly</code>
///   <para>Emits only the XPath expression for use with custom tooling.</para>
/// </example>
[Cmdlet(VerbsCommon.Get, "EVXFilter")]
[Alias("Get-EventViewerXFilter", "Get-WinEventFilter", "Get-EventsFilter")]
[OutputType(typeof(string))]
public sealed class CmdletGetEVXFilter : AsyncPSCmdlet {
    /// <summary>
    /// Event identifiers to include in the filter.
    /// </summary>
    [Parameter]
    public string[] ID { get; set; }

    /// <summary>
    /// Event record identifiers to include in the filter.
    /// </summary>
    [Alias("RecordID")]
    [Parameter]
    public string[] EventRecordID { get; set; }

    /// <summary>
    /// Start time for the filter range.
    /// </summary>
    [Parameter]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// End time for the filter range.
    /// </summary>
    [Parameter]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Specific event data values to filter on.
    /// </summary>
    [Parameter]
    public string[] Data { get; set; }

    /// <summary>
    /// Provider names to include in the filter.
    /// </summary>
    [Parameter]
    public string[] ProviderName { get; set; }

    /// <summary>
    /// Keywords to include in the filter.
    /// </summary>
    [Parameter]
    public long[] Keywords { get; set; }

    /// <summary>
    /// Event levels to include in the filter.
    /// </summary>
    [ValidateSet("Critical", "Error", "Informational", "LogAlways", "Verbose", "Warning")]
    [Parameter]
    public string[] Level { get; set; }

    /// <summary>
    /// User identifiers to include in the filter.
    /// </summary>
    [Parameter]
    public string[] UserID { get; set; }

    /// <summary>
    /// Hashtable specifying named data filters.
    /// </summary>
    [Parameter]
    public Hashtable[] NamedDataFilter { get; set; }

    /// <summary>
    /// Hashtable specifying named data to exclude from the filter.
    /// </summary>
    [Parameter]
    public Hashtable[] NamedDataExcludeFilter { get; set; }

    /// <summary>
    /// Event identifiers to exclude from the filter.
    /// </summary>
    [Parameter]
    public string[] ExcludeID { get; set; }

    /// <summary>
    /// Name of the log associated with the filter.
    /// </summary>
    [Parameter]
    public string LogName { get; set; }

    /// <summary>
    /// Path of the log file to generate the filter for.
    /// </summary>
    [Parameter]
    public string Path { get; set; }

    /// <summary>
    /// When set, outputs only the XPath expression without formatting.
    /// </summary>
    [Parameter]
    public SwitchParameter XPathOnly { get; set; }

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

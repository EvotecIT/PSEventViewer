namespace PSEventViewer;

/// <summary>
/// Generates XPath filters for Windows Event Log queries.
/// Creates filters compatible with Get-WinEvent -FilterXPath and Event Viewer Custom Views.
/// </summary>
[Cmdlet(VerbsCommon.Get, "EVXFilter")]
[Alias("Get-EventViewerXFilter", "Get-WinEventFilter", "Get-EventsFilter")]
[OutputType(typeof(string))]
public sealed class CmdletGetEVXFilter : AsyncPSCmdlet {
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

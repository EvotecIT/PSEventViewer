namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Queries or accepts EventViewerX events and creates polished HTML, Excel, or email output.</para>
/// <para type="description">Show-EVXEvent uses one normalized report snapshot for every selected output. A Type owns its source channels and event IDs; LogName is reserved for generic event queries.</para>
/// </summary>
/// <example>
///   <summary>Open a failed-logon report</summary>
///   <code>Show-EVXEvent -Type ADUserLogonFailed -TimePeriod Last24Hours</code>
///   <para>Queries the definition-owned Security events and opens a self-contained interactive HTML report.</para>
/// </example>
/// <example>
///   <summary>Create HTML and Excel from one query</summary>
///   <code>Show-EVXEvent -Type ActiveDirectoryAuthentication -Collector WEC01 -HtmlPath .\Auth.html -ExcelPath .\Auth.xlsx -PassThru</code>
///   <para>Reads ForwardedEvents once and renders both formats from the same snapshot.</para>
/// </example>
/// <example>
///   <summary>Render an existing pipeline</summary>
///   <code>Get-EVXEvent -LogName System -EventId 41,6008 | Show-EVXEvent -HtmlPath .\Startup.html</code>
///   <para>Does not query the event log again.</para>
/// </example>
[OutputType(typeof(EventReport))]
[OutputType(typeof(EventEmailPackage))]
[Cmdlet(VerbsCommon.Show, "EVXEvent", DefaultParameterSetName = "Type")]
public sealed class CmdletShowEVXEvent : AsyncPSCmdlet {
    private readonly List<object> _input = new();

    /// <summary>Built-in leaf or composite event definitions. Each definition owns its channels and event IDs.</summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Type")]
    public EventType[] Type { get; set; } = Array.Empty<EventType>();

    /// <summary>Generic event channel. Mutually exclusive with Type.</summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Log")]
    public string? LogName { get; set; }

    /// <summary>One or more offline EVTX files. Type or Definition may be supplied to apply typed semantics; Path alone creates a generic report.</summary>
    [Alias("PSPath")]
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Path", ValueFromPipelineByPropertyName = true)]
    [Parameter(ParameterSetName = "Type")]
    [Parameter(ParameterSetName = "Definition")]
    public string[] Path { get; set; } = Array.Empty<string>();

    /// <summary>Custom JSON definition path or an EventDefinition instance.</summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Definition")]
    public object? Definition { get; set; }

    /// <summary>Optional event IDs for a generic LogName query.</summary>
    [Alias("Id")]
    [Parameter(ParameterSetName = "Log")]
    [Parameter(ParameterSetName = "Path")]
    public int[]? EventId { get; set; }

    /// <summary>Exact event record identifiers, including IDs passed by an event-triggered scheduled task.</summary>
    [Alias("RecordId")]
    [Parameter(ParameterSetName = "Type")]
    [Parameter(ParameterSetName = "Log")]
    [Parameter(ParameterSetName = "Path")]
    [Parameter(ParameterSetName = "Definition")]
    public long[]? EventRecordId { get; set; }

    /// <summary>Existing EventObject or EventTypeRecord values. No source query is performed.</summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Input")]
    public object? InputObject { get; set; }

    /// <summary>Direct local or remote query targets.</summary>
    [Alias("ComputerName", "ServerName")]
    [Parameter(ParameterSetName = "Type")]
    [Parameter(ParameterSetName = "Log")]
    [Parameter(ParameterSetName = "Definition")]
    public string?[]? MachineName { get; set; }

    /// <summary>Windows Event Collector targets. Typed source channels are matched inside ForwardedEvents.</summary>
    [Parameter(ParameterSetName = "Type")]
    [Parameter(ParameterSetName = "Definition")]
    public string?[]? Collector { get; set; }

    /// <summary>Absolute start time.</summary>
    [Alias("DateFrom")]
    [Parameter(ParameterSetName = "Type")]
    [Parameter(ParameterSetName = "Log")]
    [Parameter(ParameterSetName = "Path")]
    [Parameter(ParameterSetName = "Definition")]
    public DateTime? StartTime { get; set; }

    /// <summary>Absolute end time.</summary>
    [Alias("DateTo")]
    [Parameter(ParameterSetName = "Type")]
    [Parameter(ParameterSetName = "Log")]
    [Parameter(ParameterSetName = "Path")]
    [Parameter(ParameterSetName = "Definition")]
    public DateTime? EndTime { get; set; }

    /// <summary>Relative time window.</summary>
    [Parameter(ParameterSetName = "Type")]
    [Parameter(ParameterSetName = "Log")]
    [Parameter(ParameterSetName = "Path")]
    [Parameter(ParameterSetName = "Definition")]
    public TimePeriod? TimePeriod { get; set; }

    /// <summary>Maximum report rows. Zero is unlimited.</summary>
    [ValidateRange(0, long.MaxValue)]
    [Parameter(ParameterSetName = "Type")]
    [Parameter(ParameterSetName = "Log")]
    [Parameter(ParameterSetName = "Path")]
    [Parameter(ParameterSetName = "Definition")]
    public long MaxEvents { get; set; }

    /// <summary>Maximum raw candidates evaluated by typed definitions. Zero is unlimited.</summary>
    [ValidateRange(0, long.MaxValue)]
    [Parameter(ParameterSetName = "Type")]
    [Parameter(ParameterSetName = "Definition")]
    public long MaxEventsScanned { get; set; }

    /// <summary>Maximum sources opened concurrently.</summary>
    [ValidateRange(1, EventLogLimits.MaximumConcurrency)]
    [Parameter(ParameterSetName = "Type")]
    [Parameter(ParameterSetName = "Log")]
    [Parameter(ParameterSetName = "Path")]
    [Parameter(ParameterSetName = "Definition")]
    public int MaxConcurrency { get; set; } = 8;

    /// <summary>Reads oldest matches first.</summary>
    [Parameter(ParameterSetName = "Type")]
    [Parameter(ParameterSetName = "Log")]
    [Parameter(ParameterSetName = "Path")]
    [Parameter(ParameterSetName = "Definition")]
    public SwitchParameter Oldest { get; set; }

    /// <summary>Enriches typed IP-address properties through DnsClientX.</summary>
    [Parameter(ParameterSetName = "Type")]
    public SwitchParameter ResolveDns { get; set; }

    /// <summary>Remote query credential.</summary>
    [Credential]
    [Parameter(ParameterSetName = "Type")]
    [Parameter(ParameterSetName = "Log")]
    [Parameter(ParameterSetName = "Definition")]
    public PSCredential? Credential { get; set; }

    /// <summary>Remote Windows Event Log authentication package.</summary>
    [Parameter(ParameterSetName = "Type")]
    [Parameter(ParameterSetName = "Log")]
    [Parameter(ParameterSetName = "Definition")]
    public EventLogAuthentication Authentication { get; set; }

    /// <summary>Report title.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Self-contained interactive HTML output path.</summary>
    [Parameter]
    public string? HtmlPath { get; set; }

    /// <summary>Excel workbook output path.</summary>
    [Parameter]
    public string? ExcelPath { get; set; }

    /// <summary>Returns a responsive transport-neutral email package for Mailozaurr.</summary>
    [Parameter]
    public SwitchParameter EmailPackage { get; set; }

    /// <summary>Opens generated files with the registered desktop applications.</summary>
    [Parameter]
    public SwitchParameter Open { get; set; }

    /// <summary>Returns the normalized report snapshot in addition to generated output.</summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override Task ProcessRecordAsync() {
        if (ParameterSetName == "Input" && InputObject != null) {
            object value = InputObject;
            while (value is PSObject wrapper && wrapper.BaseObject != value) {
                value = wrapper.BaseObject;
            }
            _input.Add(value);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task EndProcessingAsync() {
        EventReport report;
        if (ParameterSetName == "Input") {
            report = EventReportEngine.Create(_input, Title);
        } else {
            EventReportRequest request;
            if (ParameterSetName == "Type") {
                request = EventReportRequest.ForTypes(Type);
                request.Paths = Path.Length == 0 ? null : Path;
            } else if (ParameterSetName == "Definition") {
                object? definitionValue = Definition;
                while (definitionValue is PSObject wrapper && wrapper.BaseObject != definitionValue) {
                    definitionValue = wrapper.BaseObject;
                }
                EventDefinition definition = definitionValue switch {
                    EventDefinition typed => typed,
                    string path => EventDefinition.Load(path),
                    _ => throw new PSArgumentException("Definition must be an EventDefinition instance or a JSON file path.", nameof(Definition))
                };
                request = EventReportRequest.ForDefinition(definition);
                request.Paths = Path.Length == 0 ? null : Path;
            } else if (ParameterSetName == "Path") {
                request = EventReportRequest.ForFiles(Path);
            } else {
                request = EventReportRequest.ForLog(LogName!);
            }
            request.EventIds = EventId;
            request.RecordIds = EventRecordId;
            request.MachineNames = MachineName;
            request.Collectors = Collector;
            request.StartTime = StartTime;
            request.EndTime = EndTime;
            request.TimePeriod = TimePeriod;
            request.MaxEvents = MaxEvents;
            request.MaxCandidates = MaxEventsScanned;
            request.MaxConcurrency = MaxConcurrency;
            request.Oldest = Oldest.IsPresent;
            request.ResolveDns = ResolveDns.IsPresent;
            request.Credential = Credential?.GetNetworkCredential();
            request.Authentication = Authentication;
            request.Title = Title;
            report = await EventReportEngine.QueryAsync(request, CancelToken).ConfigureAwait(false);
        }

        bool hasDestination = !string.IsNullOrWhiteSpace(HtmlPath) || !string.IsNullOrWhiteSpace(ExcelPath) || EmailPackage.IsPresent;
        string? htmlPath = HtmlPath;
        if (!hasDestination && !PassThru.IsPresent) {
            htmlPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"EventViewerX-{DateTime.Now:yyyyMMdd-HHmmss}.html");
        }
        if (!string.IsNullOrWhiteSpace(htmlPath)) {
            string saved = EventReportHtmlRenderer.Save(report, htmlPath!, Open.IsPresent || !hasDestination);
            WriteObject(saved);
        }
        if (!string.IsNullOrWhiteSpace(ExcelPath)) {
            string saved = EventReportExcelRenderer.Save(report, ExcelPath!);
            if (Open.IsPresent) {
                Process.Start(new ProcessStartInfo(saved) { UseShellExecute = true });
            }
            WriteObject(saved);
        }
        if (EmailPackage.IsPresent) {
            WriteObject(await EventReportEmailRenderer.RenderAsync(report).ConfigureAwait(false));
        }
        if (PassThru.IsPresent) {
            WriteObject(report);
        }
    }
}

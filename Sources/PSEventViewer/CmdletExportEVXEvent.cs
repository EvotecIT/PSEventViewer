using System.Globalization;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Streams an offline Windows event log directly to CSV, JSON Lines, or XML.</para>
/// <para type="description">Uses the EventViewerX native engine and writes directly to the destination without materializing PowerShell objects. Completed output is promoted atomically, so cancellation or failure does not replace an existing file.</para>
/// </summary>
/// <example>
///   <summary>Export a complete EVTX file as JSON Lines with English messages</summary>
///   <code>Export-EVXEvent -Path C:\Logs\Security.evtx -OutputPath C:\Exports\Security.jsonl -Format JsonLines -MessageCulture en-US</code>
///   <para>Streams complete projected events directly to one JSON object per line.</para>
/// </example>
/// <example>
///   <summary>Export only core metadata at maximum throughput</summary>
///   <code>Export-EVXEvent -Path C:\Logs\System.evtx -OutputPath C:\Exports\System.csv -Format Csv -ReadMode Metadata</code>
///   <para>Skips provider messages, XML, and payload parsing while writing a stable CSV schema.</para>
/// </example>
/// <example>
///   <summary>Export filtered raw event XML</summary>
///   <code>Export-EVXEvent -Path C:\Logs\Application.evtx -OutputPath C:\Exports\Errors.xml -Format Xml -ReadMode StructuredData -XPath "*[System[Level=2]]"</code>
///   <para>Writes matching native event XML fragments inside one well-formed Events document.</para>
/// </example>
[OutputType(typeof(EventExportResult))]
[Cmdlet(VerbsData.Export, "EVXEvent", SupportsShouldProcess = true)]
public sealed class CmdletExportEVXEvent : PSCmdlet {
    private readonly CancellationTokenSource _cancellation = new();

    /// <summary>
    /// Path to the source EVTX, EVT, or ETL event log file.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    [Alias("LiteralPath")]
    public string Path { get; set; } = null!;

    /// <summary>
    /// Destination path. The parent directory must already exist.
    /// </summary>
    [Parameter(Mandatory = true, Position = 1)]
    public string OutputPath { get; set; } = null!;

    /// <summary>
    /// Direct streaming format written by the native engine.
    /// </summary>
    [Parameter]
    public EventExportFormat Format { get; set; } = EventExportFormat.JsonLines;

    /// <summary>
    /// Amount of event data projected into each exported record.
    /// </summary>
    [Parameter]
    public EventReadMode ReadMode { get; set; } = EventReadMode.Full;

    /// <summary>
    /// Native Windows event XPath expression. The default selects every record.
    /// </summary>
    [Parameter]
    public string XPath { get; set; } = "*";

    /// <summary>
    /// Returns records from oldest to newest.
    /// </summary>
    [Parameter]
    public SwitchParameter Oldest { get; set; }

    /// <summary>
    /// Maximum number of records written. Zero writes every match.
    /// </summary>
    [Parameter]
    [ValidateRange(0, int.MaxValue)]
    public int MaxEvents { get; set; }

    /// <summary>
    /// Culture used for provider messages and display names, for example en-US.
    /// </summary>
    [Parameter]
    public CultureInfo? MessageCulture { get; set; }

    /// <summary>
    /// Replaces an existing destination only after the new export completes successfully.
    /// </summary>
    [Parameter]
    public SwitchParameter Force { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord() {
        if (!ShouldProcess(OutputPath, $"Export events from '{Path}'")) {
            return;
        }

        var query = new EventLogFileQuery(Path) {
            XPath = XPath,
            Oldest = Oldest.IsPresent,
            ReadMode = ReadMode,
            MaxEvents = MaxEvents,
            MessageCulture = MessageCulture
        };

        EventExportResult result = EventLogExporter.ExportFile(
            query,
            OutputPath,
            Format,
            Force.IsPresent,
            _cancellation.Token);
        WriteObject(result);
    }

    /// <inheritdoc />
    protected override void StopProcessing() {
        _cancellation.Cancel();
        base.StopProcessing();
    }

    /// <inheritdoc />
    protected override void EndProcessing() {
        _cancellation.Dispose();
        base.EndProcessing();
    }
}

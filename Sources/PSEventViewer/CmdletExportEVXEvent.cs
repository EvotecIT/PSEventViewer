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
///   <code>Export-EVXEvent -Path C:\Logs\Application.evtx -OutputPath C:\Exports\Errors.xml -Format Xml -XPath "*[System[Level=2]]"</code>
///   <para>Writes matching native event XML fragments inside one well-formed Events document.</para>
/// </example>
/// <example>
///   <summary>Export English messages directly from a remote Security log</summary>
///   <code>Export-EVXEvent -LogName Security -MachineName DC1 -OutputPath C:\Exports\DC1-Security.jsonl -Format JsonLines -ReadMode Full -MessageCulture en-US</code>
///   <para>Uses the bounded native remote reader and avoids a PowerShell object-to-file pipeline.</para>
/// </example>
[OutputType(typeof(EventExportResult))]
[Cmdlet(
    VerbsData.Export,
    "EVXEvent",
    DefaultParameterSetName = "File",
    SupportsShouldProcess = true)]
public sealed class CmdletExportEVXEvent : PSCmdlet {
    private readonly CancellationTokenSource _cancellation = new();

    /// <summary>
    /// Path to an offline log accepted by the Windows Event Log API. EVTX is the validated format.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "File")]
    [Alias("LiteralPath")]
    public string Path { get; set; } = null!;

    /// <summary>
    /// Local or remote Windows event channel name.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Channel")]
    public string LogName { get; set; } = null!;

    /// <summary>
    /// Remote computer name. Omit to export the local channel.
    /// </summary>
    [Parameter(ParameterSetName = "Channel")]
    [Alias("ComputerName", "ServerName")]
    public string? MachineName { get; set; }

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
    /// Amount of event data projected into CSV or JSON Lines records.
    /// XML always streams the raw native event XML and ignores this value.
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
    /// Maximum time for remote RPC probing, worker admission, and session establishment.
    /// </summary>
    [Parameter(ParameterSetName = "Channel")]
    [ValidateRange(1, int.MaxValue)]
    public int RemoteConnectionTimeoutMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Maximum time without remote read progress. Zero keeps the read unbounded.
    /// </summary>
    [Parameter(ParameterSetName = "Channel")]
    [ValidateRange(0, int.MaxValue)]
    public int RemoteReadTimeoutMilliseconds { get; set; }

    /// <summary>
    /// Maximum number of detached remote events buffered between the native reader and exporter.
    /// </summary>
    [Parameter(ParameterSetName = "Channel")]
    [ValidateRange(1, 4096)]
    public int BufferCapacity { get; set; } = 64;

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

    /// <summary>
    /// Skips the final SHA-256 pass. Use this for maximum throughput when another system
    /// already provides integrity validation.
    /// </summary>
    [Parameter]
    public SwitchParameter SkipHash { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord() {
        string source = ParameterSetName == "Channel"
            ? string.IsNullOrWhiteSpace(MachineName)
                ? LogName
                : $"{MachineName}\\{LogName}"
            : Path;
        if (!ShouldProcess(OutputPath, $"Export events from '{source}'")) {
            return;
        }

        EventExportResult result;
        if (ParameterSetName == "Channel") {
            var query = new EventLogChannelQuery(LogName) {
                MachineName = MachineName,
                XPath = XPath,
                Oldest = Oldest.IsPresent,
                ReadMode = ReadMode,
                MaxEvents = MaxEvents,
                MessageCulture = MessageCulture,
                RemoteConnectionTimeoutMilliseconds =
                    RemoteConnectionTimeoutMilliseconds,
                RemoteReadTimeoutMilliseconds = RemoteReadTimeoutMilliseconds,
                BufferCapacity = BufferCapacity
            };
            result = EventLogExporter.ExportChannel(
                query,
                OutputPath,
                Format,
                Force.IsPresent,
                _cancellation.Token,
                computeSha256: !SkipHash.IsPresent);
        } else {
            var query = new EventLogFileQuery(Path) {
                XPath = XPath,
                Oldest = Oldest.IsPresent,
                ReadMode = ReadMode,
                MaxEvents = MaxEvents,
                MessageCulture = MessageCulture
            };
            result = EventLogExporter.ExportFile(
                query,
                OutputPath,
                Format,
                Force.IsPresent,
                _cancellation.Token,
                computeSha256: !SkipHash.IsPresent);
        }
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

using System.Globalization;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Streams Windows events directly to CSV, JSON Lines, XML, or native EVTX.</para>
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
    private readonly ConcurrentQueue<ErrorRecord>
        _structuredFailures = new();

    /// <summary>
    /// Path to an offline log accepted by the Windows Event Log API. EVTX is the validated format.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "File")]
    [Alias("LiteralPath")]
    public string[] Path { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Local or remote Windows event channel name.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Channel")]
    public string[] LogName { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Complete QueryList XML for a direct multi-channel or multi-file export.
    /// </summary>
    [Parameter(
        Mandatory = true,
        Position = 0,
        ParameterSetName = "Structured")]
    public string FilterXml { get; set; } = null!;

    /// <summary>
    /// Remote computer name. Omit to export the local channel.
    /// </summary>
    [Parameter(ParameterSetName = "Channel")]
    [Parameter(ParameterSetName = "Structured")]
    [Alias("ComputerName", "ServerName")]
    public string? MachineName { get; set; }

    /// <summary>Credentials for a remote channel export.</summary>
    [Credential]
    [Parameter(ParameterSetName = "Channel")]
    [Parameter(ParameterSetName = "Structured")]
    public PSCredential? Credential { get; set; }

    /// <summary>Authentication package for a remote channel export.</summary>
    [Parameter(ParameterSetName = "Channel")]
    [Parameter(ParameterSetName = "Structured")]
    public EventLogAuthentication Authentication { get; set; }

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
    [Parameter(ParameterSetName = "File")]
    [Parameter(ParameterSetName = "Channel")]
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
    [ValidateRange(0, long.MaxValue)]
    public long MaxEvents { get; set; }

    /// <summary>
    /// Maximum time for remote RPC probing, worker admission, and session establishment.
    /// </summary>
    [Parameter(ParameterSetName = "Channel")]
    [Parameter(ParameterSetName = "Structured")]
    [ValidateRange(1, int.MaxValue)]
    public int RemoteConnectionTimeoutMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Maximum time without remote read progress. Zero keeps the read unbounded.
    /// </summary>
    [Parameter(ParameterSetName = "Channel")]
    [Parameter(ParameterSetName = "Structured")]
    [ValidateRange(0, int.MaxValue)]
    public int RemoteReadTimeoutMilliseconds { get; set; }

    /// <summary>
    /// Maximum number of detached remote events buffered between the native reader and exporter.
    /// </summary>
    [Parameter(ParameterSetName = "Channel")]
    [Parameter(ParameterSetName = "Structured")]
    [ValidateRange(1, 4096)]
    public int BufferCapacity { get; set; } = 64;

    /// <summary>
    /// Culture used for provider messages and display names, for example en-US.
    /// </summary>
    [Parameter]
    public CultureInfo? MessageCulture { get; set; } =
        CultureInfo.GetCultureInfo("en-US");

    /// <summary>
    /// Culture used when provider resources do not contain MessageCulture.
    /// </summary>
    [Parameter]
    public CultureInfo? FallbackMessageCulture { get; set; } =
        CultureInfo.CurrentUICulture;

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

    /// <summary>
    /// Embeds provider resources into a native EVTX export so messages can be rendered
    /// on computers where the original providers are not installed.
    /// </summary>
    [Parameter]
    public SwitchParameter ArchiveResources { get; set; }

    /// <summary>
    /// Allows a structured QueryList export to continue when one path cannot be evaluated.
    /// </summary>
    [Parameter(ParameterSetName = "Structured")]
    public SwitchParameter TolerateQueryErrors { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord() {
        if (Format != EventExportFormat.Evtx &&
            ArchiveResources) {
            throw new PSArgumentException(
                "ArchiveResources is only valid with Format Evtx.");
        }
        if (ParameterSetName == "File" &&
            Credential != null) {
            throw new PSArgumentException(
                "Credential can only be used with a remote channel export.");
        }
        if ((ParameterSetName == "Channel" ||
             ParameterSetName == "Structured") &&
            EventLogTarget.IsLocalMachine(MachineName) &&
            Credential != null) {
            throw new PSArgumentException(
                "Credential can only be used with a remote MachineName.");
        }
        string source = ParameterSetName switch {
            "Channel" => string.IsNullOrWhiteSpace(MachineName)
                ? string.Join(", ", LogName)
                : $"{MachineName}\\{string.Join(", ", LogName)}",
            "Structured" => "structured QueryList",
            _ => string.Join(", ", Path)
        };
        if (!ShouldProcess(OutputPath, $"Export events from '{source}'")) {
            return;
        }

        EventExportResult result;
        if (ParameterSetName == "Channel") {
            string[] channels = ResolveChannels();
            if (channels.Length == 1) {
                result = EventLogExporter.ExportChannel(
                    CreateChannelQuery(channels[0]),
                    OutputPath,
                    Format,
                    Force.IsPresent,
                    _cancellation.Token,
                    computeSha256: !SkipHash.IsPresent,
                    archiveResources: ArchiveResources.IsPresent);
            } else if (Format == EventExportFormat.Evtx) {
                EventLogStructuredQuery query =
                    CreateStructuredQuery(
                        EventLogStructuredQuery
                            .ForChannels(channels, XPath));
                result = EventLogExporter.ExportStructured(
                    query,
                    OutputPath,
                    Format,
                    Force.IsPresent,
                    _cancellation.Token,
                    computeSha256: !SkipHash.IsPresent,
                    archiveResources:
                        ArchiveResources.IsPresent);
            } else {
                EventLogBatchQuery batch =
                    EventLogBatchQuery.ForChannels(
                        channels.Select(CreateChannelQuery));
                batch.MaxEvents = MaxEvents;
                result = EventLogExporter.ExportBatch(
                    batch,
                    OutputPath,
                    Format,
                    Force.IsPresent,
                    _cancellation.Token,
                    computeSha256: !SkipHash.IsPresent);
            }
        } else if (ParameterSetName == "Structured") {
            EventLogStructuredQuery query =
                CreateStructuredQuery(
                    new EventLogStructuredQuery(FilterXml));
            result = EventLogExporter.ExportStructured(
                query,
                OutputPath,
                Format,
                Force.IsPresent,
                _cancellation.Token,
                computeSha256: !SkipHash.IsPresent,
                archiveResources:
                    ArchiveResources.IsPresent);
        } else {
            string[] paths = ResolvePaths();
            if (paths.Length == 1) {
                result = EventLogExporter.ExportFile(
                    CreateFileQuery(paths[0]),
                    OutputPath,
                    Format,
                    Force.IsPresent,
                    _cancellation.Token,
                    computeSha256: !SkipHash.IsPresent,
                    archiveResources: ArchiveResources.IsPresent);
            } else {
                if (Format == EventExportFormat.Evtx) {
                    throw new PSNotSupportedException(
                        "Windows cannot merge several offline event-log files into one native EVTX. Export each source separately or choose Csv, JsonLines, or Xml.");
                }
                EventLogBatchQuery batch =
                    EventLogBatchQuery.ForFiles(
                        paths.Select(CreateFileQuery));
                batch.MaxEvents = MaxEvents;
                result = EventLogExporter.ExportBatch(
                    batch,
                    OutputPath,
                    Format,
                    Force.IsPresent,
                    _cancellation.Token,
                    computeSha256: !SkipHash.IsPresent);
            }
        }
        WriteStructuredFailures();
        WriteObject(result);
    }

    private EventLogChannelQuery CreateChannelQuery(string logName) {
        return new EventLogChannelQuery(logName) {
            MachineName = MachineName,
            Credential = Credential?.GetNetworkCredential(),
            Authentication = Authentication,
            XPath = XPath,
            Oldest = Oldest.IsPresent,
            ReadMode = ReadMode,
            MaxEvents = MaxEvents,
            MessageCulture = MessageCulture,
            FallbackMessageCulture = FallbackMessageCulture,
            RemoteConnectionTimeoutMilliseconds =
                RemoteConnectionTimeoutMilliseconds,
            RemoteReadTimeoutMilliseconds =
                RemoteReadTimeoutMilliseconds,
            BufferCapacity = BufferCapacity
        };
    }

    private EventLogFileQuery CreateFileQuery(string path) {
        return new EventLogFileQuery(path) {
            XPath = XPath,
            Oldest = Oldest.IsPresent,
            ReadMode = ReadMode,
            MaxEvents = MaxEvents,
            MessageCulture = MessageCulture,
            FallbackMessageCulture = FallbackMessageCulture
        };
    }

    private EventLogStructuredQuery CreateStructuredQuery(
        EventLogStructuredQuery query) {

        query.MachineName = MachineName;
        query.Credential = Credential?.GetNetworkCredential();
        query.Authentication = Authentication;
        query.Oldest = Oldest.IsPresent;
        query.ReadMode = ReadMode;
        query.MaxEvents = MaxEvents;
        query.MessageCulture = MessageCulture;
        query.FallbackMessageCulture =
            FallbackMessageCulture;
        query.RemoteConnectionTimeoutMilliseconds =
            RemoteConnectionTimeoutMilliseconds;
        query.RemoteReadTimeoutMilliseconds =
            RemoteReadTimeoutMilliseconds;
        query.BufferCapacity = BufferCapacity;
        query.TolerateQueryErrors =
            TolerateQueryErrors.IsPresent;
        query.FailureHandler = failure =>
            _structuredFailures.Enqueue(new ErrorRecord(
                failure.Exception,
                "EVXStructuredExportPathFailed",
                ErrorCategory.ReadError,
                failure.Source));
        return query;
    }

    private void WriteStructuredFailures() {
        while (_structuredFailures.TryDequeue(
                   out ErrorRecord? failure)) {
            WriteError(failure);
        }
    }

    private string[] ResolveChannels() {
        var query = new EventLogCatalogQuery {
            MachineName = MachineName,
            Credential = Credential?.GetNetworkCredential(),
            Authentication = Authentication,
            ConnectionTimeoutMilliseconds =
                RemoteConnectionTimeoutMilliseconds,
            Culture = MessageCulture
        };
        string[] channels = EventLogCatalog
            .GetChannelNames(
                query,
                LogName,
                cancellationToken: _cancellation.Token)
            .ToArray();
        if (channels.Length == 0) {
            throw new ItemNotFoundException(
                $"No event channels match '{string.Join(", ", LogName)}' on '{MachineName ?? Environment.MachineName}'.");
        }
        return channels;
    }

    private string[] ResolvePaths() {
        var paths = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        foreach (string pattern in Path) {
            try {
                foreach (string path in SessionState.Path
                             .GetResolvedProviderPathFromPSPath(
                                 pattern,
                                 out ProviderInfo provider)) {
                    if (!string.Equals(
                            provider.Name,
                            "FileSystem",
                            StringComparison.OrdinalIgnoreCase)) {
                        throw new PSArgumentException(
                            $"Path '{pattern}' must use the FileSystem provider.");
                    }
                    paths.Add(System.IO.Path.GetFullPath(path));
                }
            } catch (ItemNotFoundException) {
                throw new PSArgumentException(
                    $"No event-log files match path '{pattern}'.");
            }
        }
        if (paths.Count == 0) {
            throw new PSArgumentException(
                "At least one event-log file is required.");
        }
        return paths
            .OrderBy(static path =>
                path,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Enhanced event querying cmdlet that replaces and extends Get-WinEvent functionality.</para>
/// <para type="description">Supports local/remote logs, named event shortcuts, record ID resumes, parallel queries, and rich filtering (IDs, providers, keywords, levels, time windows, named data).</para>
/// </summary>
/// <example>
///   <summary>Query successful logons from Security</summary>
///   <code>Get-EVXEvent -LogName Security -EventId 4624 -StartTime (Get-Date).AddHours(-1)</code>
///   <para>Shows only successful logons from the last hour.</para>
/// </example>
/// <example>
///   <summary>Read events from an EVTX file</summary>
///   <code>Get-EVXEvent -Path C:\Logs\App.evtx -EventId 1000,1001</code>
///   <para>Filters specific application error IDs from an offline log.</para>
/// </example>
/// <example>
///   <summary>Resume from last record ID</summary>
///   <code>Get-EVXEvent -LogName Security -RecordIdFile C:\temp\resume.json -RecordIdKey Sec</code>
///   <para>Continues from the last processed record and updates the checkpoint file.</para>
/// </example>
/// <example>
///   <summary>Use named event shortcuts</summary>
///   <code>Get-EVXEvent -NamedEvents ADUserLogonFailed -StartTime (Get-Date).AddDays(-1)</code>
///   <para>Expands the named event definition to fetch all related logon failure IDs.</para>
/// </example>
/// <example>
///   <summary>Parallel query across servers</summary>
///   <code>Get-EVXEvent -LogName Security -MachineName DC1,DC2 -EventId 4740 -MaxConcurrency 8</code>
///   <para>Retrieves account lockouts from multiple domain controllers with bounded concurrent source setup.</para>
/// </example>
/// <example>
///   <summary>Stream core metadata from a large EVTX file</summary>
///   <code>Get-EVXEvent -Path C:\Logs\Security.evtx -Oldest -ReadMode Metadata | Select-Object TimeCreated, RecordId, Id, ProviderName, MachineName | Export-Csv C:\Logs\Security-metadata.csv -NoTypeInformation</code>
///   <para>Skips provider message formatting, XML parsing, attachments, and bookmarks while streaming every record.</para>
/// </example>
[OutputType(typeof(EventObject), ParameterSetName = new string[] { "GenericEvents" })]
[OutputType(typeof(EventObject), ParameterSetName = new string[] { "PathEvents" })]
[OutputType(typeof(EventObject), ParameterSetName = new string[] { "FilterHashtableEvents" })]
[OutputType(typeof(EventObject), ParameterSetName = new string[] { "FilterXmlEvents" })]
[OutputType(typeof(EventObject), ParameterSetName = new string[] { "ProviderEvents" })]
[OutputType(typeof(EventObjectSlim), ParameterSetName = new string[] { "NamedEvents" })]
[Cmdlet(VerbsCommon.Get, "EVXEvent", DefaultParameterSetName = "ProviderEvents")]
[Alias("Find-WinEvent")]
public sealed partial class CmdletGetEVXEvent : AsyncPSCmdlet {
    private string _recordIdKey = string.Empty;
    private Dictionary<string, long> _recordMap = new();
    private readonly Dictionary<string, Guid> _checkpointGenerations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _checkpointBoundaries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _checkpointBoundaryMigrations = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<CheckpointSource>? _checkpointSources;
    private readonly Dictionary<string, long> _highestRecordIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, EventObject> _highestCheckpointEvents = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _resetCheckpointKeys = new(StringComparer.OrdinalIgnoreCase);
    private WildcardPattern[] _managedProviderPatterns =
        Array.Empty<WildcardPattern>();
    private long _eventsOutput;
    /// <summary>
    /// Name of the log to query.
    /// </summary>
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true,
        ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    public string[] LogName { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Path to an event log file for offline analysis.
    /// </summary>
    [Alias("PSPath")]
    [Parameter(
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        ParameterSetName = "PathEvents")]
    public string[] Path { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Event identifiers used to filter results.
    /// </summary>
    [Alias("Id")]
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "ProviderEvents")]
    public int[]? EventId { get; set; }

    /// <summary>
    /// Specific event record identifiers to retrieve.
    /// </summary>
    [Alias("RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public long[]? EventRecordId { get; set; }

    /// <summary>
    /// Path to a file storing last processed record ID.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public string? RecordIdFile { get; set; }

    /// <summary>
    /// Identifier used when persisting record IDs to allow multiple jobs to share a file.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public string? RecordIdKey { get; set; }

    /// <summary>
    /// Computer names against which to run the query.
    /// </summary>
    [Alias("ComputerName", "ServerName")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public List<string?>? MachineName { get; set; }

    /// <summary>
    /// Event provider name to filter results.
    /// </summary>
    [Alias("Source", "Provider")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = true, ParameterSetName = "ProviderEvents")]
    public string[]? ProviderName { get; set; }

    /// <summary>
    /// Keywords used to filter events.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public long[]? Keywords { get; set; }

    /// <summary>
    /// Event level (e.g. Error, Warning) used for filtering.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public int[]? Level { get; set; }

    /// <summary>
    /// Start time for the event query.
    /// </summary>
    [Alias("DateFrom")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// End time for the event query.
    /// </summary>
    [Alias("DateTo")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Relative time period for filtering events.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public TimePeriod? TimePeriod { get; set; }

    /// <summary>
    /// User identifier used to filter events.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public string[]? UserId { get; set; }

    /// <summary>
    /// Filters events by matching their formatted message against the provided regular expression.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public Regex? MessageRegex { get; set; }

    /// <summary>
    /// Maximum number of independent event sources opened concurrently.
    /// </summary>
    [Alias("NumberOfThreads")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    [ValidateRange(1, EventLogLimits.MaximumConcurrency)]
    public int MaxConcurrency { get; set; } = 8;

    /// <summary>
    /// Maximum number of events to return.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    [ValidateRange(0, long.MaxValue)]
    public long MaxEvents { get; set; }

    /// <summary>
    /// Maximum number of merged candidate events delivered for message and checkpoint filtering.
    /// Zero continues until the output limit is satisfied or the query is exhausted. Native selection may perform
    /// one initial lookahead per machine/XPath chunk plus bounded page prefetch; those rows are not evaluated by the cmdlet.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    [ValidateRange(0, long.MaxValue)]
    public long MaxEventsScanned { get; set; }

    /// <summary>
    /// Resolves reverse-DNS names for supported named events after projection. DNS failures remain visible on the
    /// event and never remove the event from the pipeline.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    public SwitchParameter ResolveDns { get; set; }

    /// <summary>
    /// Whole-request timeout in milliseconds for each optional reverse-DNS request, including dependency retries.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [ValidateRange(1, 60000)]
    public int DnsTimeoutMs { get; set; } = 1000;

    /// <summary>
    /// Maximum number of reverse-DNS requests that may overlap. Results and checkpoints remain in event order.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [ValidateRange(1, 64)]
    public int DnsMaxConcurrency { get; set; } = 8;

    /// <summary>
    /// Controls per-event materialization. Metadata skips provider messages, XML, attachments, and bookmarks;
    /// Message formats the provider message; StructuredData parses XML without formatting the message; Full includes all data.
    /// Named-event queries default to Full so rule projections receive their structured payload; other query sets default to Message.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public EventReadMode ReadMode { get; set; } =
        EventReadMode.Message;

    /// <summary>
    /// Culture used to format provider messages and display names for offline EVTX queries.
    /// For example, use <c>en-US</c> for deterministic English output.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public CultureInfo? MessageCulture { get; set; } =
        CultureInfo.GetCultureInfo("en-US");

    /// <summary>
    /// Overrides both remote connection and no-progress read timeouts in milliseconds.
    /// Zero uses Settings.SessionTimeoutMs for connection establishment and
    /// Settings.QuerySessionTimeoutMs for reading.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    [ValidateRange(0, int.MaxValue)]
    public int SessionTimeoutMs { get; set; }

    private int EffectiveRemoteConnectionTimeoutMilliseconds =>
        SessionTimeoutMs > 0
            ? SessionTimeoutMs
            : Settings.SessionTimeoutMs;

    private int EffectiveRemoteReadTimeoutMilliseconds =>
        SessionTimeoutMs > 0
            ? SessionTimeoutMs
            : Settings.QuerySessionTimeoutMs;

    /// <summary>
    /// Maximum number of projected events buffered between parallel readers and the PowerShell pipeline. Zero selects a bounded default.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    [ValidateRange(0, int.MaxValue)]
    public int BufferCapacity { get; set; }

    /// <summary>
    /// Expands event data into individual properties.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public SwitchParameter Expand { get; set; }

    /// <summary>
    /// Reads events from oldest to newest when querying files.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public SwitchParameter Oldest { get; set; }

    /// <summary>
    /// Hashtable filter for named event data when querying files.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public Hashtable? NamedDataFilter { get; set; }

    /// <summary>
    /// Hashtable filter to exclude named event data when querying files.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public Hashtable? NamedDataExcludeFilter { get; set; }

    /// <summary>
    /// Disables parallel processing of queries.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public SwitchParameter DisableParallel { get; set; }

    /// <summary>
    /// Returns results as an array instead of streaming them.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterHashtableEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "FilterXmlEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ProviderEvents")]
    public SwitchParameter AsArray { get; set; }

    /// <summary>
    /// Predefined named events to query.
    /// </summary>
    [Alias("NamedEvents")]
    [Parameter(Mandatory = true, ParameterSetName = "NamedEvents")]
    public NamedEvents[] Type { get; set; } = Array.Empty<NamedEvents>();

    /// <summary>
    /// Initializes logging and helper classes before processing.
    /// </summary>
    protected override async Task ProcessRecordAsync() {
        _eventsOutput = 0;
        _checkpointSources = null;
        _managedProviderPatterns =
            Array.Empty<WildcardPattern>();
        _offlineProvidersByPath.Clear();
        if (ParameterSetName == "NamedEvents" &&
            !MyInvocation.BoundParameters.ContainsKey(
                nameof(ReadMode))) {
            ReadMode = EventReadMode.Full;
        }
        ValidateRecordOptions();
        InitializeCheckpointKey();

        CancellationToken token;
#if NET8_0_OR_GREATER
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(StoppingToken, CancelToken);
        token = linkedCts.Token;
#else
        token = CancelToken;
#endif
        List<object>? results = AsArray ? new List<object>() : null;

        PrepareRecordProcessing(token);
        if (ParameterSetName == "NamedEvents") {
                // let's find the events prepared for search
                List<NamedEvents> typeList = Type.ToList();
                int namedEventThreads = DisableParallel.IsPresent
                    ? 1
                    : MaxConcurrency;
                var namedQueryInfo = new NamedEventsQueryExecutionInfo();
                Func<EventObjectSlim, bool>? namedResultPredicate = MessageRegex == null
                    ? null
                    : eventObject => MessageMatches(eventObject.Event);
                NamedEventEnrichmentOptions? enrichmentOptions = ResolveDns
                    ? new NamedEventEnrichmentOptions {
                        ResolveDns = true,
                        DnsTimeoutMilliseconds = DnsTimeoutMs,
                        DnsMaxConcurrency = DnsMaxConcurrency,
                        RetryDnsOnTransient = false
                    }
                    : null;
                var namedQuery =
                    new NamedEventQuery(typeList) {
                        MachineNames = MachineName,
                        StartTime = StartTime,
                        EndTime = EndTime,
                        TimePeriod = TimePeriod,
                        SourceLogName =
                            LogName.SingleOrDefault(),
                        SourceEventIds =
                            EventId,
                        MaxConcurrency =
                            namedEventThreads,
                        MaxEvents = MaxEvents,
                        MaxCandidates =
                            MaxEventsScanned,
                        MinimumRecordIdExclusiveResolver =
                            GetCheckpointLowerBound,
                        CandidateObserver =
                            candidate =>
                                TrackCheckpointProgress(
                                    candidate),
                        Oldest = EffectiveOldest,
                        ReadMode =
                            ReadMode,
                        ResultPredicate =
                            namedResultPredicate,
                        Enrichment =
                            enrichmentOptions,
                        MessageCulture =
                            MessageCulture,
                        FallbackMessageCulture =
                            FallbackMessageCulture,
                        Credential =
                            Credential?.GetNetworkCredential(),
                        Authentication =
                            Authentication,
                        RemoteConnectionTimeoutMilliseconds =
                            EffectiveRemoteConnectionTimeoutMilliseconds,
                        RemoteReadTimeoutMilliseconds =
                            EffectiveRemoteReadTimeoutMilliseconds,
                        BufferCapacity =
                            BufferCapacity > 0
                                ? BufferCapacity
                                : 64,
                        ContinueOnRemoteFailure =
                            ContinueOnError.IsPresent ||
                            (MachineName?.Count ?? 0) > 1,
                        IncludeBookmark =
                            IncludeBookmark.IsPresent
                    };
                await foreach (EventObjectSlim eventObject in
                               NamedEventEngine.ReadAsync(
                                   namedQuery,
                                   namedQueryInfo,
                                   token)) {
                    token.ThrowIfCancellationRequested();
                    if (!TrackCheckpointProgress(eventObject.Event)) {
                        continue;
                    }
                    object output = Expand
                        ? GetExpandedObject(eventObject, eventObject.Event)
                        : eventObject;
                    if (AsArray) {
                        results!.Add(output);
                    } else {
                        WriteObject(output);
                    }
                    _eventsOutput++;
                    if (OutputLimitReached) {
                        break;
                    }
                }
        } else {
            ProcessNativeEvents(token, results);
        }

        WriteArrayResult(results);
    }

    private bool HasManagedPostReadFilter =>
        MessageRegex != null ||
        _managedProviderPatterns.Length > 0 ||
        UsesCheckpoint;

    private bool UsesManagedOutputSelection =>
        (MessageRegex != null ||
         _managedProviderPatterns.Length > 0) &&
        !UsesCheckpoint &&
        MaxEvents > 0 &&
        MaxEventsScanned <= 0;

    private void ProcessEventResult(EventObject eventObject, List<object>? results) {
        if (!TrackCheckpointProgress(eventObject) ||
            !ProviderMatches(eventObject) ||
            !MessageMatches(eventObject)) {
            return;
        }

        object output = Expand ? GetExpandedObject(eventObject) : eventObject;
        if (AsArray) {
            results!.Add(output);
        } else {
            WriteObject(output);
        }
        _eventsOutput++;
    }

    /// <summary>
    /// Creates an expanded PSObject from EventObject with properties expanded from the Data property.
    /// </summary>
    /// <param name="eventObject">The event object.</param>
    /// <returns>PSObject with expanded properties.</returns>
    private PSObject GetExpandedObject(EventObject eventObject) {
        return GetExpandedObject(eventObject, eventObject);
    }

    /// <summary>
    /// Creates an expanded PSObject around the selected output projection using structured data from its source event.
    /// </summary>
    /// <param name="output">Object that remains the PowerShell base object.</param>
    /// <param name="eventObject">Source event whose structured data is expanded.</param>
    /// <returns>PSObject with non-conflicting structured data properties.</returns>
    private static PSObject GetExpandedObject(object output, EventObject eventObject) {
        PSObject outputObj = new(output);
        foreach (var property in eventObject.Data.OrderBy(static d => d.Key, StringComparer.OrdinalIgnoreCase)) {
            if (outputObj.Properties[property.Key] == null) {
                outputObj.Properties.Add(new PSNoteProperty(property.Key, property.Value));
            }
        }
        return outputObj;
    }

    /// <summary>
    /// Checks whether the event object's formatted message matches the provided regex filter.
    /// </summary>
    /// <param name="eventObject">The event to test.</param>
    /// <returns>True when no regex is defined or when the message matches the expression.</returns>
    private bool MessageMatches(EventObject eventObject) {
        if (MessageRegex == null) {
            return true;
        }

        var message = eventObject?.Message ?? string.Empty;
        return MessageRegex.IsMatch(message);
    }

    private bool ProviderMatches(EventObject eventObject) {
        return _managedProviderPatterns.Length == 0 ||
               _managedProviderPatterns.Any(pattern =>
                   pattern.IsMatch(
                       eventObject.ProviderName ??
                       string.Empty));
    }

}

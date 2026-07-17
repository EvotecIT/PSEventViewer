using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Net;

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
///   <code>Get-EVXEvent -LogName Security -MachineName DC1,DC2 -EventId 4740 -Parallel</code>
///   <para>Retrieves account lockouts from multiple domain controllers concurrently.</para>
/// </example>
[OutputType(typeof(EventObject), ParameterSetName = new string[] { "GenericEvents" })]
[OutputType(typeof(EventObject), ParameterSetName = new string[] { "PathEvents" })]
[OutputType(typeof(EventObjectSlim), ParameterSetName = new string[] { "NamedEvents" })]
[OutputType(typeof(EventLogDetails), ParameterSetName = new string[] { "ListLog" })]
[Cmdlet(VerbsCommon.Get, "EVXEvent", DefaultParameterSetName = "GenericEvents")]
[Alias("Get-EventViewerXEvent", "Find-WinEvent", "Get-Events")]
public sealed class CmdletGetEVXEvent : AsyncPSCmdlet {
    private string _recordIdKey = string.Empty;
    private Dictionary<string, long> _recordMap = new();
    private readonly Dictionary<string, long> _highestRecordIds = new(StringComparer.OrdinalIgnoreCase);
    private int _eventsOutput;
    /// <summary>
    /// Name of the log to query.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "GenericEvents")]
    public string LogName { get; set; } = null!;

    /// <summary>
    /// Path to an event log file for offline analysis.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "PathEvents")]
    public string Path { get; set; } = null!;

    /// <summary>
    /// Event identifiers used to filter results.
    /// </summary>
    [Alias("Id")]
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public List<int>? EventId { get; set; }

    /// <summary>
    /// Specific event record identifiers to retrieve.
    /// </summary>
    [Alias("RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public List<long>? EventRecordId { get; set; }

    /// <summary>
    /// Path to a file storing last processed record ID.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public string? RecordIdFile { get; set; }

    /// <summary>
    /// Identifier used when persisting record IDs to allow multiple jobs to share a file.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public string? RecordIdKey { get; set; }

    /// <summary>
    /// Computer names against which to run the query.
    /// </summary>
    [Alias("ComputerName", "ServerName")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ListLog")]
    public List<string?>? MachineName { get; set; }

    /// <summary>
    /// Event provider name to filter results.
    /// </summary>
    [Alias("Source", "Provider")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public string? ProviderName { get; set; }

    /// <summary>
    /// Keywords used to filter events.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public Keywords? Keywords { get; set; }

    /// <summary>
    /// Event level (e.g. Error, Warning) used for filtering.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public Level? Level { get; set; }

    /// <summary>
    /// Start time for the event query.
    /// </summary>
    [Alias("DateFrom")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// End time for the event query.
    /// </summary>
    [Alias("DateTo")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Relative time period for filtering events.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public TimePeriod? TimePeriod { get; set; }

    /// <summary>
    /// User identifier used to filter events.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public string? UserId { get; set; }

    /// <summary>
    /// Filters events by matching their formatted message against the provided regular expression.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public Regex? MessageRegex { get; set; }

    /// <summary>
    /// Number of parallel threads used for queries.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ListLog")]
    [ValidateRange(1, SearchEvents.MaximumParallelism)]
    public int NumberOfThreads { get; set; } = 8;

    /// <summary>
    /// Maximum number of events to return.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [ValidateRange(0, int.MaxValue)]
    public int MaxEvents { get; set; }

    /// <summary>
    /// Maximum number of candidate events to scan before applying message and checkpoint filters. Zero scans until the output limit is satisfied or the query is exhausted.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [ValidateRange(0, int.MaxValue)]
    public int MaxEventsScanned { get; set; }

    /// <summary>
    /// Controls whether each event includes metadata only, the formatted message, structured XML data, or all data.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public EventReadMode ReadMode { get; set; } = EventReadMode.Full;

    /// <summary>
    /// Session and per-read timeout in milliseconds. Zero keeps the legacy unbounded read behavior.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [ValidateRange(0, int.MaxValue)]
    public int SessionTimeoutMs { get; set; }

    /// <summary>
    /// Maximum number of projected events buffered between parallel readers and the PowerShell pipeline. Zero selects a bounded default.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [ValidateRange(0, int.MaxValue)]
    public int BufferCapacity { get; set; }

    /// <summary>
    /// Controls whether queries run in parallel or sequentially.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    public ParallelOption ParallelOption { get; set; } = ParallelOption.Parallel;

    /// <summary>
    /// Expands event data into individual properties.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public SwitchParameter Expand { get; set; }

    /// <summary>
    /// Reads events from oldest to newest when querying files.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public SwitchParameter Oldest { get; set; }

    /// <summary>
    /// Hashtable filter for named event data when querying files.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public Hashtable? NamedDataFilter { get; set; }

    /// <summary>
    /// Hashtable filter to exclude named event data when querying files.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public Hashtable? NamedDataExcludeFilter { get; set; }

    /// <summary>
    /// Disables parallel processing of queries.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    public SwitchParameter DisableParallel { get; set; }

    /// <summary>
    /// Returns results as an array instead of streaming them.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ListLog")]
    public SwitchParameter AsArray { get; set; }

    /// <summary>
    /// Predefined named events to query.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "NamedEvents")]
    public NamedEvents[] Type { get; set; } = Array.Empty<NamedEvents>();

    /// <summary>
    /// The list log parameter is used to list the logs on the machine.
    /// You can use wildcards to search for logs.
    /// When using wildcards, you can use the * character to match zero or more characters, and the ? character to match a single character.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "ListLog")]
    public string[] ListLog { get; set; } = new[] { "*" };


    /// <summary>
    /// Initializes logging and helper classes before processing.
    /// </summary>
    protected override Task BeginProcessingAsync() {
        // Initialize the logger to be able to see verbose, warning, debug, error, progress, and information messages.
        var internalLogger = new InternalLogger(false);
        var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
        SetEventViewerLogger(internalLogger);
        var searchEvents = new SearchEvents(internalLogger);
        if (!string.IsNullOrEmpty(RecordIdFile) && File.Exists(RecordIdFile)) {
            _recordMap = ReadCheckpointFile(RecordIdFile!);
        }
        _recordIdKey = !string.IsNullOrEmpty(RecordIdKey)
            ? RecordIdKey!
            : BuildDefaultCheckpointKey();
        if (string.IsNullOrEmpty(RecordIdKey)) {
            string legacyKey = BuildLegacyCheckpointKey();
            if (!_recordMap.ContainsKey(_recordIdKey) && _recordMap.TryGetValue(legacyKey, out long legacyRecordId)) {
                _recordMap[_recordIdKey] = legacyRecordId;
            }
        }
        return Task.CompletedTask;
    }

    private string BuildDefaultCheckpointKey() {
        string queryIdentity = ParameterSetName switch {
            "NamedEvents" => "Named:" + string.Join(",", Type.OrderBy(static value => value)),
            "PathEvents" => "Path:" + Path,
            _ => "Log:" + (LogName ?? string.Empty)
        };
        string machines = string.Join(",", MachineName ?? new List<string?>());
        return $"{queryIdentity}|{machines}";
    }

    private string BuildLegacyCheckpointKey() {
        string queryIdentity = LogName ?? Path ?? "unknown";
        string machines = string.Join(",", MachineName ?? new List<string?>());
        return $"{queryIdentity}|{machines}";
    }
    /// <summary>
    /// Executes the event query based on provided parameters.
    /// </summary>
    protected override async Task ProcessRecordAsync() {
        if (Expand && ReadMode != EventReadMode.StructuredData && ReadMode != EventReadMode.Full) {
            throw new PSArgumentException("-Expand requires -ReadMode StructuredData or Full.");
        }
        if (MessageRegex != null && ReadMode != EventReadMode.Message && ReadMode != EventReadMode.Full) {
            throw new PSArgumentException("-MessageRegex requires -ReadMode Message or Full.");
        }

        CancellationToken token;
#if NET8_0_OR_GREATER
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(StoppingToken, CancelToken);
        token = linkedCts.Token;
#else
        token = CancelToken;
#endif
        List<object>? results = AsArray ? new List<object>() : null;

        if (DisableParallel.IsPresent) {
            ParallelOption = ParallelOption.Disabled;
        }

        PrepareCheckpointBounds(token);

        if (ParameterSetName == "ListLog") {
            foreach (EventLogDetails log in SearchEvents.DisplayEventLogsParallel(ListLog, MachineName, NumberOfThreads, token)) {
                token.ThrowIfCancellationRequested();
                if (AsArray) {
                    results!.Add(log);
                } else {
                    WriteObject(log);
                }
            }
        } else if (ParameterSetName == "PathEvents") {
            foreach (EventObject eventObject in SearchEvents.QueryLogFile(Path, EventId, ProviderName, Keywords, Level, StartTime, EndTime, UserId, GetQueryReadLimit(), EventRecordId, TimePeriod, Oldest, NamedDataFilter, NamedDataExcludeFilter, token, ReadMode, GetCheckpointLowerBound(null, Path))) {
                token.ThrowIfCancellationRequested();
                ProcessEventResult(eventObject, results);
                if (OutputLimitReached) {
                    break;
                }
            }
        } else {
            if (ParameterSetName == "NamedEvents") {
                // let's find the events prepared for search
                List<NamedEvents> typeList = Type.ToList();
                int namedEventThreads = ParallelOption == ParallelOption.Disabled ? 1 : NumberOfThreads;
                int namedEventMatchLimit = HasManagedPostReadFilter ? 0 : MaxEvents;
                await foreach (EventObjectSlim eventObject in SearchEvents.FindEventsByNamedEvents(
                                   typeList,
                                   MachineName,
                                   StartTime,
                                   EndTime,
                                   TimePeriod,
                                   maxThreads: namedEventThreads,
                                   maxEvents: namedEventMatchLimit,
                                   maxEventsScanned: MaxEventsScanned,
                                   cancellationToken: token,
                                   minimumEventRecordIdExclusiveResolver: GetCheckpointLowerBound,
                                   candidateObserver: candidate => TrackCheckpointProgress(candidate))) {
                    token.ThrowIfCancellationRequested();
                    if (!TrackCheckpointProgress(eventObject.Event) || !MessageMatches(eventObject.Event)) {
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
            } else if (ParallelOption == ParallelOption.Disabled) {
                foreach (EventObject eventObject in SearchEvents.QueryLogsSequential(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, GetQueryReadLimit(), EventRecordId, TimePeriod, token, SessionTimeoutMs, ReadMode, GetCheckpointResolver(LogName))) {
                    token.ThrowIfCancellationRequested();
                    ProcessEventResult(eventObject, results);
                    if (OutputLimitReached) {
                        break;
                    }
                }
            } else {
                await foreach (EventObject eventObject in SearchEvents.QueryLogsParallel(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, GetQueryReadLimit(), NumberOfThreads, EventRecordId, TimePeriod, token, SessionTimeoutMs, ReadMode, BufferCapacity, GetCheckpointResolver(LogName))) {
                    token.ThrowIfCancellationRequested();
                    ProcessEventResult(eventObject, results);
                    if (OutputLimitReached) {
                        break;
                    }
                }
            }
        }

        // If AsArray is specified, output all results as an array
        if (AsArray && results != null) {
            WriteObject(results.ToArray(), false);
        }

    }

    private bool TrackCheckpointProgress(EventObject eventObject) {
        if (!eventObject.RecordId.HasValue) {
            return true;
        }

        string checkpointKey = GetCheckpointKey(eventObject);
        long recordId = eventObject.RecordId.Value;
        bool hasCheckpoint = _recordMap.TryGetValue(checkpointKey, out long previousRecordId);
        if (!hasCheckpoint && !string.Equals(checkpointKey, _recordIdKey, StringComparison.OrdinalIgnoreCase)) {
            hasCheckpoint = _recordMap.TryGetValue(_recordIdKey, out previousRecordId);
        }
        if (hasCheckpoint && recordId <= previousRecordId) {
            return false;
        }
        if (!_highestRecordIds.TryGetValue(checkpointKey, out long highestRecordId) || recordId > highestRecordId) {
            _highestRecordIds[checkpointKey] = recordId;
        }
        return true;
    }

    private string GetCheckpointKey(EventObject eventObject) {
        bool hasMultipleSources = ParameterSetName == "NamedEvents" || (MachineName?.Count ?? 0) > 1;
        if (!hasMultipleSources) {
            return _recordIdKey;
        }

        string source = string.IsNullOrWhiteSpace(eventObject.QueriedMachine)
            ? eventObject.MachineName
            : eventObject.QueriedMachine;
        return $"{_recordIdKey}|{source}|{eventObject.ContainerLog}";
    }

    private Func<string?, long?>? GetCheckpointResolver(string logName) {
        if (string.IsNullOrWhiteSpace(RecordIdFile)) {
            return null;
        }
        return machineName => GetCheckpointLowerBound(machineName, logName);
    }

    private long? GetCheckpointLowerBound(string? machineName, string logName) {
        if (string.IsNullOrWhiteSpace(RecordIdFile)) {
            return null;
        }

        return TryGetCheckpoint(machineName, logName, out _, out long checkpoint)
            ? checkpoint
            : null;
    }

    private bool TryGetCheckpoint(string? machineName, string logName, out string checkpointKey, out long checkpoint) {
        checkpointKey = _recordIdKey;
        checkpoint = 0;

        bool hasMultipleSources = ParameterSetName == "NamedEvents" || (MachineName?.Count ?? 0) > 1;
        if (!hasMultipleSources) {
            return _recordMap.TryGetValue(_recordIdKey, out checkpoint);
        }

        HashSet<string> sourceNames = GetCheckpointSourceNames(machineName);
        foreach (string sourceName in sourceNames) {
            string sourceKey = $"{_recordIdKey}|{sourceName}|{logName}";
            if (_recordMap.TryGetValue(sourceKey, out checkpoint)) {
                checkpointKey = sourceKey;
                return true;
            }
        }

        return _recordMap.TryGetValue(_recordIdKey, out checkpoint);
    }

    private static HashSet<string> GetCheckpointSourceNames(string? machineName) {
        var sourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(machineName)) {
            sourceNames.Add(machineName!.Trim());
        } else {
            sourceNames.Add(Environment.MachineName);
            try {
                sourceNames.Add(Dns.GetHostEntry(Environment.MachineName).HostName);
            } catch (System.Net.Sockets.SocketException) {
                // The short local name remains a valid fallback when DNS is unavailable.
            }
        }
        return sourceNames;
    }

    private void PrepareCheckpointBounds(CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(RecordIdFile) || _recordMap.Count == 0 || ParameterSetName == "ListLog") {
            return;
        }

        if (ParameterSetName == "PathEvents") {
            if (TryGetCheckpoint(null, Path, out string checkpointKey, out long checkpoint)) {
                EventObject? newest = SearchEvents.QueryLogFile(
                    Path,
                    maxEvents: 1,
                    cancellationToken: cancellationToken,
                    readMode: EventReadMode.Metadata).FirstOrDefault();
                ResetCheckpointWhenLogRestarted(checkpointKey, checkpoint, newest?.RecordId, Path);
            }
            return;
        }

        IEnumerable<string> logs = ParameterSetName == "NamedEvents"
            ? EventObjectSlim.GetEventInfoForNamedEvents(Type.ToList()).Keys
            : new[] { LogName };
        IEnumerable<string?> machines = MachineName == null || MachineName.Count == 0
            ? new string?[] { null }
            : MachineName;

        foreach (string log in logs) {
            foreach (string? machine in machines) {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryGetCheckpoint(machine, log, out string checkpointKey, out long checkpoint)) {
                    continue;
                }

                try {
                    EventObject? newest = SearchEvents.QueryLog(
                        log,
                        machineName: machine,
                        maxEvents: 1,
                        cancellationToken: cancellationToken,
                        sessionTimeoutMs: ParameterSetName == "GenericEvents" ? SessionTimeoutMs : null,
                        readMode: EventReadMode.Metadata).FirstOrDefault();
                    ResetCheckpointWhenLogRestarted(
                        checkpointKey,
                        checkpoint,
                        newest?.RecordId,
                        string.IsNullOrWhiteSpace(machine) ? log : $"{log} on {machine}");
                } catch (Exception ex) when (EventLogRemoteQueryFailureClassifier.TryClassify(machine, ex, out _)) {
                    WriteVerbose($"Checkpoint generation probe skipped unavailable target '{machine}': {ex.Message}");
                }
            }
        }
    }

    private void ResetCheckpointWhenLogRestarted(
        string checkpointKey,
        long checkpoint,
        long? newestRecordId,
        string target) {

        if (!newestRecordId.HasValue || newestRecordId.Value >= checkpoint) {
            return;
        }

        _recordMap.Remove(checkpointKey);
        _highestRecordIds.Remove(checkpointKey);
        WriteWarning(
            $"Checkpoint '{checkpointKey}' was {checkpoint}, but the newest record in '{target}' is {newestRecordId.Value}. " +
            "The log was cleared or replaced; restarting this source from its current records.");
    }

    private bool OutputLimitReached => MaxEvents > 0 && _eventsOutput >= MaxEvents;

    private int GetQueryReadLimit() {
        if (HasManagedPostReadFilter || MaxEvents <= 0) {
            return MaxEventsScanned;
        }
        if (MaxEventsScanned <= 0) {
            return MaxEvents;
        }
        return Math.Min(MaxEvents, MaxEventsScanned);
    }

    private bool HasManagedPostReadFilter => MessageRegex != null || !string.IsNullOrEmpty(RecordIdFile);

    private void ProcessEventResult(EventObject eventObject, List<object>? results) {
        if (!TrackCheckpointProgress(eventObject) || !MessageMatches(eventObject)) {
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

    /// <summary>
    /// Saves the highest processed record ID to <see cref="RecordIdFile"/> when processing completes.
    /// </summary>
    protected override Task EndProcessingAsync() {
        if (!string.IsNullOrEmpty(RecordIdFile) && _highestRecordIds.Count > 0) {
            string checkpointPath = System.IO.Path.GetFullPath(RecordIdFile!);
            using var checkpointMutex = new Mutex(false, BuildCheckpointMutexName(checkpointPath));
            bool acquired = false;
            try {
                try {
                    acquired = checkpointMutex.WaitOne(TimeSpan.FromSeconds(30));
                } catch (AbandonedMutexException) {
                    acquired = true;
                }

                if (!acquired) {
                    throw new TimeoutException($"Timed out waiting to update shared event checkpoint file '{checkpointPath}'.");
                }

                Dictionary<string, long> latestMap = ReadCheckpointFile(checkpointPath);
                foreach (KeyValuePair<string, long> checkpoint in _highestRecordIds) {
                    if (!latestMap.TryGetValue(checkpoint.Key, out long existing) || checkpoint.Value > existing) {
                        latestMap[checkpoint.Key] = checkpoint.Value;
                    }
                }
                _recordMap = latestMap;
                WriteCheckpointFile(checkpointPath, JsonSerializer.Serialize(latestMap));
            } finally {
                if (acquired) {
                    checkpointMutex.ReleaseMutex();
                }
            }
        }
        return Task.CompletedTask;
    }

    private static Dictionary<string, long> ReadCheckpointFile(string path) {
        try {
            if (!File.Exists(path)) {
                return new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            }

            string json = File.ReadAllText(path);
            Dictionary<string, long>? persistedMap = JsonSerializer.Deserialize<Dictionary<string, long>>(json);
            return persistedMap == null
                ? new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, long>(persistedMap, StringComparer.OrdinalIgnoreCase);
        } catch (FileNotFoundException) {
            return new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        } catch (DirectoryNotFoundException) {
            return new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        } catch (JsonException) {
            return new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string BuildCheckpointMutexName(string checkpointPath) {
        string identity = checkpointPath.ToUpperInvariant();
        using SHA256 sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(identity));
        string suffix = BitConverter.ToString(hash).Replace("-", string.Empty);
        return $"Local\\PSEventViewer.Checkpoint.{suffix}";
    }

    private static void WriteCheckpointFile(string path, string contents) {
        string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            File.WriteAllText(temporaryPath, contents);
            if (File.Exists(path)) {
                File.Replace(temporaryPath, path, null);
            } else {
                File.Move(temporaryPath, path);
            }
        } finally {
            if (File.Exists(temporaryPath)) {
                File.Delete(temporaryPath);
            }
        }
    }
}

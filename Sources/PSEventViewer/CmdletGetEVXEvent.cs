using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

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
    private long? _resumeRecordId;
    private long? _highestRecordId;
    private string _recordIdKey = string.Empty;
    private Dictionary<string, long> _recordMap = new();
    /// <summary>
    /// Name of the log to query.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "GenericEvents")]
    public string LogName { get; set; }

    /// <summary>
    /// Path to an event log file for offline analysis.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "PathEvents")]
    public string Path { get; set; }

    /// <summary>
    /// Event identifiers used to filter results.
    /// </summary>
    [Alias("Id")]
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public List<int> EventId { get; set; }

    /// <summary>
    /// Specific event record identifiers to retrieve.
    /// </summary>
    [Alias("RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    public List<long> EventRecordId { get; set; }

    /// <summary>
    /// Path to a file storing last processed record ID.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public string RecordIdFile { get; set; }

    /// <summary>
    /// Identifier used when persisting record IDs to allow multiple jobs to share a file.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public string RecordIdKey { get; set; }

    /// <summary>
    /// Computer names against which to run the query.
    /// </summary>
    [Alias("ComputerName", "ServerName")]
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ListLog")]
    public List<string> MachineName { get; set; }

    /// <summary>
    /// Event provider name to filter results.
    /// </summary>
    [Alias("Source", "Provider")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public string ProviderName { get; set; }

    /// <summary>
    /// Keywords used to filter events.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public Keywords? Keywords { get; set; }

    /// <summary>
    /// Event level (e.g. Error, Warning) used for filtering.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
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
    public TimePeriod? TimePeriod { get; set; }

    /// <summary>
    /// User identifier used to filter events.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public string UserId { get; set; }

    /// <summary>
    /// Filters events by matching their formatted message against the provided regular expression.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public Regex MessageRegex { get; set; }

    /// <summary>
    /// Number of parallel threads used for queries.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [ValidateRange(1, int.MaxValue)]
    public int NumberOfThreads { get; set; } = 8;

    /// <summary>
    /// Maximum number of events to return.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public int MaxEvents { get; set; }

    /// <summary>
    /// Controls whether queries run in parallel or sequentially.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public ParallelOption ParallelOption { get; set; } = ParallelOption.Parallel;

    /// <summary>
    /// Expands event data into individual properties.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
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
    public Hashtable NamedDataFilter { get; set; }

    /// <summary>
    /// Hashtable filter to exclude named event data when querying files.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public Hashtable NamedDataExcludeFilter { get; set; }

    /// <summary>
    /// Disables parallel processing of queries.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    public SwitchParameter DisableParallel { get; set; }

    /// <summary>
    /// Returns results as an array instead of streaming them.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
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
        var searchEvents = new SearchEvents(internalLogger);
        if (!string.IsNullOrEmpty(RecordIdFile) && File.Exists(RecordIdFile)) {
            try {
                var json = File.ReadAllText(RecordIdFile);
                _recordMap = JsonSerializer.Deserialize<Dictionary<string, long>>(json) ?? new();
            } catch {
                _recordMap = new Dictionary<string, long>();
            }
        }
        _recordIdKey = !string.IsNullOrEmpty(RecordIdKey)
            ? RecordIdKey
            : $"{LogName ?? Path ?? "unknown"}|{string.Join(",", MachineName ?? new List<string>())}";
        if (_recordMap.TryGetValue(_recordIdKey, out var lastId)) {
            _resumeRecordId = lastId;
        }
        return Task.CompletedTask;
    }
    /// <summary>
    /// Executes the event query based on provided parameters.
    /// </summary>
    protected override async Task ProcessRecordAsync() {
        CancellationToken token;
#if NET8_0_OR_GREATER
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(StoppingToken, CancelToken);
        token = linkedCts.Token;
#else
        token = CancelToken;
#endif
        List<object> results = AsArray ? new List<object>() : null;

        if (DisableParallel.IsPresent) {
            ParallelOption = ParallelOption.Disabled;
        }

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
            // Handle file path queries
            if (Expand == false) {
                foreach (EventObject eventObject in SearchEvents.QueryLogFile(Path, EventId, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, null, TimePeriod, Oldest, NamedDataFilter, NamedDataExcludeFilter, token)) {
                    token.ThrowIfCancellationRequested();
                    if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                        continue;
                    }
                    if (AsArray) {
                        results!.Add(eventObject);
                    } else {
                        WriteObject(eventObject);
                    }
                }
            } else {
                foreach (EventObject eventObject in SearchEvents.QueryLogFile(Path, EventId, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, null, TimePeriod, Oldest, NamedDataFilter, NamedDataExcludeFilter, token)) {
                    token.ThrowIfCancellationRequested();
                    if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                        continue;
                    }
                    if (AsArray) {
                        results!.Add(GetExpandedObject(eventObject));
                    } else {
                        ReturnExpandedObject(eventObject);
                    }
                }
            }
        } else {
            if (Type != null) {
                // let's find the events prepared for search
                List<NamedEvents> typeList = Type.ToList();
                await foreach (EventObjectSlim eventObject in SearchEvents.FindEventsByNamedEvents(typeList, MachineName, StartTime, EndTime, TimePeriod, maxThreads: NumberOfThreads, maxEvents: MaxEvents, cancellationToken: token)) {
                    token.ThrowIfCancellationRequested();
                    if (!MessageMatches(eventObject._eventObject) || !ShouldOutput(eventObject._eventObject)) {
                        continue;
                    }
                    if (AsArray) {
                        results!.Add(eventObject);
                    } else {
                        WriteObject(eventObject);
                    }
                }
            } else {
                if (Expand == false) {
                    // Let's find the events by generic log name, event id, machine name, provider name, keywords, level, start time, end time, user id, and max events.
                    if (ParallelOption == ParallelOption.Disabled) {
                        if (MachineName == null) {
                            foreach (EventObject eventObject in SearchEvents.QueryLog(LogName, EventId, null, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod, token)) {
                                token.ThrowIfCancellationRequested();
                                if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                                    continue;
                                }
                                if (AsArray) {
                                    results!.Add(eventObject);
                                } else {
                                    WriteObject(eventObject);
                                }
                            }
                        } else {
                            foreach (string machine in MachineName) {
                                token.ThrowIfCancellationRequested();
                                foreach (EventObject eventObject in SearchEvents.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod, token)) {
                                    token.ThrowIfCancellationRequested();
                                    if (!MessageMatches(eventObject)) {
                                        continue;
                                    }
                                    if (AsArray) {
                                        results!.Add(eventObject);
                                    } else {
                                        WriteObject(eventObject);
                                    }
                                }
                            }
                        }
                    } else if (ParallelOption == ParallelOption.Parallel) {
                        await foreach (EventObject eventObject in SearchEvents.QueryLogsParallel(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, NumberOfThreads, EventRecordId, TimePeriod, token)) {
                            token.ThrowIfCancellationRequested();
                            if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                                continue;
                            }
                            if (AsArray) {
                                results!.Add(eventObject);
                            } else {
                                WriteObject(eventObject);
                            }
                        }
                    }
                } else {
                    // Let's find objects, but we will expand the properties of the object from Data to the PSObject.
                    if (ParallelOption == ParallelOption.Disabled) {
                        if (MachineName == null) {
                            foreach (EventObject eventObject in SearchEvents.QueryLog(LogName, EventId, null, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod, token)) {
                                token.ThrowIfCancellationRequested();
                                if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                                    continue;
                                }
                                if (AsArray) {
                                    results!.Add(GetExpandedObject(eventObject));
                                } else {
                                    ReturnExpandedObject(eventObject);
                                }
                            }
                        } else {
                            foreach (string machine in MachineName) {
                                token.ThrowIfCancellationRequested();
                                foreach (EventObject eventObject in SearchEvents.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod, token)) {
                                    token.ThrowIfCancellationRequested();
                                    if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                                        continue;
                                    }
                                    if (AsArray) {
                                        results!.Add(GetExpandedObject(eventObject));
                                    } else {
                                        ReturnExpandedObject(eventObject);
                                    }
                                }
                            }
                        }
                    } else if (ParallelOption == ParallelOption.Parallel) {
                        await foreach (EventObject eventObject in SearchEvents.QueryLogsParallel(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, NumberOfThreads, EventRecordId, TimePeriod, token)) {
                            token.ThrowIfCancellationRequested();
                            if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                                continue;
                            }
                            if (AsArray) {
                                results!.Add(GetExpandedObject(eventObject));
                            } else {
                                ReturnExpandedObject(eventObject);
                            }
                        }
                    }
                }
            }
        }

        // If AsArray is specified, output all results as an array
        if (AsArray && results != null) {
            WriteObject(results.ToArray(), false);
        }

        return;
    }

    private bool ShouldOutput(EventObject eventObject) {
        if (_resumeRecordId.HasValue && eventObject.RecordId.HasValue && eventObject.RecordId.Value <= _resumeRecordId.Value) {
            return false;
        }
        if (eventObject.RecordId.HasValue) {
            var id = eventObject.RecordId.Value;
            if (!_highestRecordId.HasValue || id > _highestRecordId.Value) {
                _highestRecordId = id;
            }
        }
        return true;
    }
    /// <summary>
    /// Returns the expanded object - it takes the EventObject and returns the PSObject with the properties expanded from the Data property.
    /// </summary>
    /// <param name="eventObject">The event object.</param>
    private void ReturnExpandedObject(EventObject eventObject) {
        PSObject outputObj = GetExpandedObject(eventObject);
        WriteObject(outputObj);
    }

    /// <summary>
    /// Creates an expanded PSObject from EventObject with properties expanded from the Data property.
    /// </summary>
    /// <param name="eventObject">The event object.</param>
    /// <returns>PSObject with expanded properties.</returns>
    private PSObject GetExpandedObject(EventObject eventObject) {
        PSObject outputObj = new(eventObject); // => it's the preferred way to create a wrapper pso when you already know it's not a pso
                                               // PSObject outputObj = PSObject.AsPSObject(eventObject); => this is the preferred way to convert from PSO to PSObject
        foreach (var property in eventObject.Data.OrderBy(static d => d.Key, StringComparer.OrdinalIgnoreCase)) {
            outputObj.Properties.Add(new PSNoteProperty(property.Key, property.Value));
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
        if (!string.IsNullOrEmpty(RecordIdFile) && _highestRecordId.HasValue) {
            _recordMap[_recordIdKey] = _highestRecordId.Value;
            File.WriteAllText(RecordIdFile, JsonSerializer.Serialize(_recordMap));
        }
        return Task.CompletedTask;
    }
}

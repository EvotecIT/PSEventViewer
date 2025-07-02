using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PSEventViewer;

/// <summary>
/// Enhanced event querying cmdlet that replaces and extends Get-WinEvent functionality.
/// Provides powerful filtering, parallel processing, and advanced event retrieval capabilities.
/// </summary>
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
    public string LogName;

    /// <summary>
    /// Path to an event log file for offline analysis.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "PathEvents")]
    public string Path;

    /// <summary>
    /// Event identifiers used to filter results.
    /// </summary>
    [Alias("Id")]
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public List<int> EventId = null;

    /// <summary>
    /// Specific event record identifiers to retrieve.
    /// </summary>
    [Alias("RecordId")]
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "RecordId")]
    public List<long> EventRecordId = null;

    /// <summary>
    /// Path to a file storing last processed record ID.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public string RecordIdFile;

    /// <summary>
    /// Identifier used when persisting record IDs to allow multiple jobs to share a file.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public string RecordIdKey;

    /// <summary>
    /// Computer names against which to run the query.
    /// </summary>
    [Alias("ComputerName", "ServerName")]
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ListLog")]
    public List<string> MachineName;

    /// <summary>
    /// Event provider name to filter results.
    /// </summary>
    [Alias("Source", "Provider")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public string ProviderName;

    /// <summary>
    /// Keywords used to filter events.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public Keywords? Keywords;

    /// <summary>
    /// Event level (e.g. Error, Warning) used for filtering.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public Level? Level;

    /// <summary>
    /// Start time for the event query.
    /// </summary>
    [Alias("DateFrom")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public DateTime? StartTime;

    /// <summary>
    /// End time for the event query.
    /// </summary>
    [Alias("DateTo")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public DateTime? EndTime;

    /// <summary>
    /// Relative time period for filtering events.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    public TimePeriod? TimePeriod;

    /// <summary>
    /// User identifier used to filter events.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public string UserId;

    /// <summary>
    /// Filters events by matching their formatted message against the provided regular expression.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public Regex MessageRegex;

    /// <summary>
    /// Number of parallel threads used for queries.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [ValidateRange(1, int.MaxValue)]
    public int NumberOfThreads = 8;

    /// <summary>
    /// Maximum number of events to return.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public int MaxEvents = 0;

    /// <summary>
    /// Controls whether queries run in parallel or sequentially.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public ParallelOption ParallelOption = ParallelOption.Parallel;

    /// <summary>
    /// Expands event data into individual properties.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public SwitchParameter Expand;

    /// <summary>
    /// Reads events from oldest to newest when querying files.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public SwitchParameter Oldest;

    /// <summary>
    /// Hashtable filter for named event data when querying files.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public Hashtable NamedDataFilter;

    /// <summary>
    /// Hashtable filter to exclude named event data when querying files.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public Hashtable NamedDataExcludeFilter;

    /// <summary>
    /// Disables parallel processing of file queries.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public SwitchParameter DisableParallel;

    /// <summary>
    /// Returns results as an array instead of streaming them.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ListLog")]
    public SwitchParameter AsArray;

    /// <summary>
    /// Predefined named events to query.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "NamedEvents")]
    public NamedEvents[] Type;

    /// <summary>
    /// The list log parameter is used to list the logs on the machine.
    /// You can use wildcards to search for logs.
    /// When using wildcards, you can use the * character to match zero or more characters, and the ? character to match a single character.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "ListLog")]
    public string[] ListLog = { "*" };


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
        var results = AsArray ? new List<object>() : null;

        if (ParameterSetName == "ListLog") {
            foreach (var log in SearchEvents.DisplayEventLogsParallel(ListLog, MachineName, NumberOfThreads)) {
                if (AsArray) {
                    results.Add(log);
                } else {
                    WriteObject(log);
                }
            }
        } else if (ParameterSetName == "PathEvents") {
            // Handle file path queries
            if (Expand == false) {
                foreach (var eventObject in SearchEvents.QueryLogFile(Path, EventId, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, null, TimePeriod, Oldest, NamedDataFilter, NamedDataExcludeFilter, CancelToken)) {
                    if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                        continue;
                    }
                    if (AsArray) {
                        results.Add(eventObject);
                    } else {
                        WriteObject(eventObject);
                    }
                }
            } else {
                foreach (var eventObject in SearchEvents.QueryLogFile(Path, EventId, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, null, TimePeriod, Oldest, NamedDataFilter, NamedDataExcludeFilter, CancelToken)) {
                    if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                        continue;
                    }
                    if (AsArray) {
                        results.Add(GetExpandedObject(eventObject));
                    } else {
                        ReturnExpandedObject(eventObject);
                    }
                }
            }
        } else {
            if (Type != null) {
                // let's find the events prepared for search
                List<NamedEvents> typeList = Type.ToList();
                await foreach (var eventObject in SearchEvents.FindEventsByNamedEvents(typeList, MachineName, StartTime, EndTime, TimePeriod, maxThreads: NumberOfThreads, maxEvents: MaxEvents, cancellationToken: CancelToken)) {
                    if (!MessageMatches(eventObject._eventObject) || !ShouldOutput(eventObject._eventObject)) {
                        continue;
                    }
                    if (AsArray) {
                        results.Add(eventObject);
                    } else {
                        WriteObject(eventObject);
                    }
                }
            } else {
                if (Expand == false) {
                    // Let's find the events by generic log name, event id, machine name, provider name, keywords, level, start time, end time, user id, and max events.
                    if (ParallelOption == ParallelOption.Disabled) {
                        if (MachineName == null) {
                            foreach (var eventObject in SearchEvents.QueryLog(LogName, EventId, null, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod, CancelToken)) {
                                if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                                    continue;
                                }
                                if (AsArray) {
                                    results.Add(eventObject);
                                } else {
                                    WriteObject(eventObject);
                                }
                            }
                        } else {
                            foreach (var machine in MachineName) {
                                foreach (var eventObject in SearchEvents.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod, CancelToken)) {
                                    if (!MessageMatches(eventObject)) {
                                        continue;
                                    }
                                    if (AsArray) {
                                        results.Add(eventObject);
                                    } else {
                                        WriteObject(eventObject);
                                    }
                                }
                            }
                        }
                    } else if (ParallelOption == ParallelOption.Parallel) {
                        await foreach (var eventObject in SearchEvents.QueryLogsParallel(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, NumberOfThreads, EventRecordId, TimePeriod, CancelToken)) {
                            if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                                continue;
                            }
                            if (AsArray) {
                                results.Add(eventObject);
                            } else {
                                WriteObject(eventObject);
                            }
                        }
                    }
                } else {
                    // Let's find objects, but we will expand the properties of the object from Data to the PSObject.
                    if (ParallelOption == ParallelOption.Disabled) {
                        if (MachineName == null) {
                            foreach (var eventObject in SearchEvents.QueryLog(LogName, EventId, null, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod, CancelToken)) {
                                if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                                    continue;
                                }
                                if (AsArray) {
                                    results.Add(GetExpandedObject(eventObject));
                                } else {
                                    ReturnExpandedObject(eventObject);
                                }
                            }
                        } else {
                            foreach (var machine in MachineName) {
                                foreach (var eventObject in SearchEvents.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod, CancelToken)) {
                                    if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                                        continue;
                                    }
                                    if (AsArray) {
                                        results.Add(GetExpandedObject(eventObject));
                                    } else {
                                        ReturnExpandedObject(eventObject);
                                    }
                                }
                            }
                        }
                    } else if (ParallelOption == ParallelOption.Parallel) {
                        await foreach (var eventObject in SearchEvents.QueryLogsParallel(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, NumberOfThreads, EventRecordId, TimePeriod, CancelToken)) {
                            if (!MessageMatches(eventObject) || !ShouldOutput(eventObject)) {
                                continue;
                            }
                            if (AsArray) {
                                results.Add(GetExpandedObject(eventObject));
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
        foreach (var property in eventObject.Data) {
            //// if the property already exists, add it as new property with +1
            //if (outputObj.Properties[property.Key] != null) {
            //    outputObj.Properties.Add(new PSNoteProperty(property.Key + "1", property.Value));
            //} else {
            //    outputObj.Properties.Add(new PSNoteProperty(property.Key, property.Value));
            //}
            //if (outputObj.Properties[property.Key] != null) {
            //    outputObj.Properties.Remove(property.Key);
            //}
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

    protected override Task EndProcessingAsync() {
        if (!string.IsNullOrEmpty(RecordIdFile) && _highestRecordId.HasValue) {
            _recordMap[_recordIdKey] = _highestRecordId.Value;
            File.WriteAllText(RecordIdFile, JsonSerializer.Serialize(_recordMap));
        }
        return Task.CompletedTask;
    }
}
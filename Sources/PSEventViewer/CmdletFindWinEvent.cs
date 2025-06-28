using System.Collections;

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
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "GenericEvents")]
    public string LogName;

    [Parameter(Mandatory = true, ParameterSetName = "PathEvents")]
    public string Path;

    [Alias("Id")]
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public List<int> EventId = null;

    [Alias("RecordId")]
    [Parameter(Mandatory = false, Position = 1, ParameterSetName = "RecordId")]
    public List<long> EventRecordId = null;

    [Alias("ComputerName", "ServerName")]
    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ListLog")]
    public List<string> MachineName;

    [Alias("Source", "Provider")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public string ProviderName;

    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public Keywords? Keywords;

    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public Level? Level;

    [Alias("DateFrom")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public DateTime? StartTime;

    [Alias("DateTo")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public DateTime? EndTime;

    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    public TimePeriod? TimePeriod;

    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public string UserId;

    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    public int NumberOfThreads = 8;

    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public int MaxEvents = 0;

    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public ParallelOption ParallelOption = ParallelOption.Parallel;

    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public SwitchParameter Expand;

    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public SwitchParameter Oldest;

    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public Hashtable NamedDataFilter;

    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public Hashtable NamedDataExcludeFilter;

    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    public SwitchParameter DisableParallel;

    [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "PathEvents")]
    [Parameter(Mandatory = false, ParameterSetName = "ListLog")]
    public SwitchParameter AsArray;

    [Parameter(Mandatory = true, ParameterSetName = "NamedEvents")]
    public NamedEvents[] Type;

    /// <summary>
    /// The list log parameter is used to list the logs on the machine.
    /// You can use wildcards to search for logs.
    /// When using wildcards, you can use the * character to match zero or more characters, and the ? character to match a single character.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "ListLog")]
    public string[] ListLog = { "*" };


    protected override Task BeginProcessingAsync() {
        // Initialize the logger to be able to see verbose, warning, debug, error, progress, and information messages.
        var internalLogger = new InternalLogger(false);
        var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
        var searchEvents = new SearchEvents(internalLogger);
        return Task.CompletedTask;
    }
    protected override Task ProcessRecordAsync() {
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
                foreach (var eventObject in SearchEvents.QueryLogFile(Path, EventId, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, null, TimePeriod, Oldest, NamedDataFilter, NamedDataExcludeFilter)) {
                    if (AsArray) {
                        results.Add(eventObject);
                    } else {
                        WriteObject(eventObject);
                    }
                }
            } else {
                foreach (var eventObject in SearchEvents.QueryLogFile(Path, EventId, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, null, TimePeriod, Oldest, NamedDataFilter, NamedDataExcludeFilter)) {
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
                foreach (var eventObject in SearchEvents.FindEventsByNamedEvents(typeList, MachineName, StartTime, EndTime, TimePeriod, maxThreads: NumberOfThreads, maxEvents: MaxEvents)) {
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
                            foreach (var eventObject in SearchEvents.QueryLog(LogName, EventId, null, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod)) {
                                if (AsArray) {
                                    results.Add(eventObject);
                                } else {
                                    WriteObject(eventObject);
                                }
                            }
                        } else {
                            foreach (var machine in MachineName) {
                                foreach (var eventObject in SearchEvents.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod)) {
                                    if (AsArray) {
                                        results.Add(eventObject);
                                    } else {
                                        WriteObject(eventObject);
                                    }
                                }
                            }
                        }
                    } else if (ParallelOption == ParallelOption.Parallel) {
                        foreach (var eventObject in SearchEvents.QueryLogsParallel(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, NumberOfThreads, EventRecordId, TimePeriod)) {
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
                            foreach (var eventObject in SearchEvents.QueryLog(LogName, EventId, null, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod)) {
                                if (AsArray) {
                                    results.Add(GetExpandedObject(eventObject));
                                } else {
                                    ReturnExpandedObject(eventObject);
                                }
                            }
                        } else {
                            foreach (var machine in MachineName) {
                                foreach (var eventObject in SearchEvents.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId, TimePeriod)) {
                                    if (AsArray) {
                                        results.Add(GetExpandedObject(eventObject));
                                    } else {
                                        ReturnExpandedObject(eventObject);
                                    }
                                }
                            }
                        }
                    } else if (ParallelOption == ParallelOption.Parallel) {
                        foreach (var eventObject in SearchEvents.QueryLogsParallel(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, NumberOfThreads, EventRecordId, TimePeriod)) {
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

        return Task.CompletedTask;
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
}

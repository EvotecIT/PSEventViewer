using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    /// <summary>
    /// Initialize the EventSearching class with an internal logger
    /// </summary>
    /// <param name="internalLogger">The internal logger.</param>
    public SearchEvents(InternalLogger internalLogger = null) {
        if (internalLogger != null) {
            _logger = internalLogger;
        }
    }

    /// <summary>
    /// Create an event log reader allowing for catching errors and logging them
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="machineName">Name of the machine.</param>
    /// <returns>Initialized <see cref="EventLogReader"/> or null when failed.</returns>
    private static EventLogReader CreateEventLogReader(EventLogQuery query, string machineName) {
        string targetMachine = string.IsNullOrEmpty(machineName) ? GetFQDN() : machineName;
        if (query == null) {
            _logger.WriteWarning($"An error occurred on {targetMachine} while creating the event log reader: Query cannot be null.");
            return null;
        }

        try {
            return new EventLogReader(query);
        } catch (EventLogException ex) {
            _logger.WriteWarning($"An error occurred on {targetMachine} while creating the event log reader: {ex.Message}");
            return null;
        } catch (UnauthorizedAccessException ex) {
            _logger.WriteWarning($"Insufficient permissions to read the event log on {targetMachine}: {ex.Message}");
            return null;
        } catch (Exception ex) {
            _logger.WriteWarning($"An error occurred on {targetMachine} while creating the event log reader: {ex.Message}");
            return null;
        }
    }
    // QueryLogFile moved to SearchEvents.QueryLogFile.cs


    private static IEnumerable<EventObject> QueryLogEnumerable(string logName, List<int> eventIds = null, string machineName = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, List<long> eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        if (eventIds != null && eventIds.Any(id => id <= 0)) {
            throw new ArgumentException("Event IDs must be positive.", nameof(eventIds));
        }

        if (eventRecordId != null && eventRecordId.Any(id => id <= 0)) {
            throw new ArgumentException("Event record IDs must be positive.", nameof(eventRecordId));
        }

        string queryString = eventRecordId != null
            ? BuildQueryString(eventRecordId)
            : BuildQueryString(logName, eventIds, providerName, keywords, level, startTime, endTime, userId, timePeriod: timePeriod);

        _logger.WriteVerbose($"Querying log '{logName}' on '{machineName} with query: {queryString}");

        EventLogQuery query = new EventLogQuery(logName, PathType.LogName, queryString);
        if (!string.IsNullOrEmpty(machineName)) {
            query.Session = new EventLogSession(machineName);
        }

        var queriedMachine = string.IsNullOrEmpty(machineName) ? GetFQDN() : machineName;

        EventRecord record;
        using (EventLogReader reader = CreateEventLogReader(query, machineName)) {
            if (reader != null) {
                int eventCount = 0;
                while (!cancellationToken.IsCancellationRequested && (record = reader.ReadEvent()) != null) {
                    EventObject eventObject = new EventObject(record, queriedMachine);
                    yield return eventObject;
                    eventCount++;
                    if (maxEvents > 0 && eventCount >= maxEvents) {
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Queries the log.
    /// </summary>
    /// <param name="logName">Name of the log.</param>
    /// <param name="eventIds">The event ids.</param>
    /// <param name="machineName">Name of the machine.</param>
    /// <param name="providerName">Name of the provider.</param>
    /// <param name="keywords">The keywords.</param>
    /// <param name="level">The level.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="endTime">The end time.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="maxEvents">The maximum events.</param>
    /// <param name="eventRecordId">The event record identifier.</param>
    /// <param name="timePeriod">The time period.</param>
    /// <returns>Enumerable collection of matching events.</returns>
    public static IEnumerable<EventObject> QueryLog(string logName, List<int> eventIds = null, string machineName = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, List<long> eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        return QueryLogEnumerable(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod, cancellationToken);
    }

    public static IEnumerable<EventObject> QueryLog(KnownLog logName, List<int> eventIds = null, string machineName = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, List<long> eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        return QueryLog(LogNameToString(logName), eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod, cancellationToken);
    }

    public static async Task<IEnumerable<EventObject>> QueryLogAsync(string logName, List<int> eventIds = null, string machineName = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, List<long> eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        return await Task.Run(() => QueryLogEnumerable(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod, cancellationToken).ToList().AsEnumerable(), cancellationToken);
    }

    /// <summary>
    /// Build a query string for querying a log for a specific event record ID or IDs
    /// </summary>
    /// <param name="eventRecordIds">Event record identifiers.</param>
    /// <returns>XML query string.</returns>
    // BuildQueryString methods moved to SearchEvents.QueryLog.BuildQueryString.cs

    private static void AddCondition(StringBuilder queryString, string condition) {
        if (!queryString.ToString().EndsWith("[System[")) {
            queryString.Append(" and ");
        }
        queryString.Append(condition);
    }

    private static string LogNameToString(KnownLog logName) => logName switch {
        KnownLog.DirectoryService => "Directory Service",
        KnownLog.DNSServer => "DNS Server",
        KnownLog.WindowsPowerShell => "Windows PowerShell",
        _ => logName.ToString()
    };

    public static async IAsyncEnumerable<EventObject> QueryLogsParallel(string logName, List<int> eventIds = null, List<string> machineNames = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, int maxThreads = 8, List<long> eventRecordId = null, TimePeriod? timePeriod = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        if (machineNames == null || !machineNames.Any()) {
            machineNames = new List<string> { null };
            _logger.WriteVerbose("No machine names provided, querying the local machine.");
        } else {
            _logger.WriteVerbose($"Machine names provided. Creating tasks for each machine on the list: {string.Join(", ", machineNames)}");
        }
        var semaphore = new SemaphoreSlim(maxThreads);
        var results = new BlockingCollection<EventObject>();

        var tasks = new List<Task>();
        foreach (var machineName in machineNames) {
            if (eventIds != null && eventIds.Any()) {
                var eventIdsChunks = eventIds.Select((x, i) => new { Index = i, Value = x })
                    .GroupBy(x => x.Index / 22)
                    .Select(x => x.Select(v => v.Value).ToList())
                    .ToList();

                foreach (var chunk in eventIdsChunks) {
                    tasks.Add(CreateTask(machineName, logName, chunk, providerName, keywords, level, startTime, endTime, userId, maxEvents, semaphore, results, cancellationToken, timePeriod: timePeriod));
                }
            } else if (eventRecordId != null && eventRecordId.Any()) {
                var eventRecordIdChunks = eventRecordId.Select((x, i) => new { Index = i, Value = x })
                    .GroupBy(x => x.Index / 22)
                    .Select(x => x.Select(v => v.Value).ToList())
                    .ToList();

                foreach (var chunk in eventRecordIdChunks) {
                    tasks.Add(CreateTask(machineName, logName, null, providerName, keywords, level, startTime, endTime, userId, maxEvents, semaphore, results, cancellationToken, chunk, timePeriod: timePeriod));
                }
            } else {
                // event ids are null or empty, so we don't need to chunk them
                tasks.Add(CreateTask(machineName, logName, eventIds, providerName, keywords, level, startTime, endTime, userId, maxEvents, semaphore, results, cancellationToken, timePeriod: timePeriod));
            }
        }

        var whenAllTask = Task.WhenAll(tasks);
        _ = whenAllTask.ContinueWith(
            _ => results.CompleteAdding(),
            cancellationToken,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        foreach (var result in results.GetConsumingEnumerable(cancellationToken)) {
            yield return result;
            await Task.Yield();
        }

        await whenAllTask;
    }

    public static async Task<IEnumerable<EventObject>> QueryLogsParallelAsync(string logName, List<int> eventIds = null, List<string> machineNames = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, int maxThreads = 8, List<long> eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        var results = new List<EventObject>();
        await foreach (var ev in QueryLogsParallel(logName, eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, timePeriod, cancellationToken)) {
            results.Add(ev);
        }
        return results;
    }

    public static IAsyncEnumerable<EventObject> QueryLogsParallel(KnownLog logName, List<int> eventIds = null, List<string> machineNames = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, int maxThreads = 8, List<long> eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        return QueryLogsParallel(LogNameToString(logName), eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, timePeriod, cancellationToken);
    }

    public static Task<IEnumerable<EventObject>> QueryLogsParallelAsync(KnownLog logName, List<int> eventIds = null, List<string> machineNames = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, int maxThreads = 8, List<long> eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        return QueryLogsParallelAsync(LogNameToString(logName), eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, timePeriod, cancellationToken);
    }

    private static Task CreateTask(string machineName, string logName, List<int> eventIds, string providerName, Keywords? keywords, Level? level, DateTime? startTime, DateTime? endTime, string userId, int maxEvents, SemaphoreSlim semaphore, BlockingCollection<EventObject> results, CancellationToken cancellationToken, List<long> eventRecordId = null, TimePeriod? timePeriod = null) {
        return Task.Run(async () => {
            await semaphore.WaitAsync(cancellationToken);
            try {
                _logger.WriteVerbose($"Querying log on machine: {machineName}, logName: {logName}, event ids: {string.Join(", ", eventIds ?? new List<int>())}");
                foreach (var result in QueryLogEnumerable(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod, cancellationToken)) {
                    if (cancellationToken.IsCancellationRequested) break;
                    results.Add(result, cancellationToken);
                }
                _logger.WriteVerbose($"Querying log on machine: {machineName} completed.");
            } finally {
                semaphore.Release();
            }
        }, cancellationToken);
    }

    public static IEnumerable<EventObject> QueryLogsParallelForEach(string logName, List<int> eventIds = null, List<string> machineNames = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, int maxThreads = 4, List<long> eventRecordId = null, CancellationToken cancellationToken = default) {
        if (machineNames == null || !machineNames.Any()) {
            throw new ArgumentException("At least one machine name must be provided", nameof(machineNames));
        }

        var results = new BlockingCollection<EventObject>();
        var exceptions = new ConcurrentQueue<Exception>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = maxThreads };

        Task workerTask = Task.Factory.StartNew(() => {
            try {
                Parallel.ForEach(machineNames, options, machineName => {
                    try {
                        _logger.WriteVerbose($"Starting task for machine: {machineName}");
                        var queryResults = QueryLog(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, cancellationToken: cancellationToken);
                        foreach (var result in queryResults) {
                            if (cancellationToken.IsCancellationRequested) break;
                            results.Add(result, cancellationToken);
                        }
                        _logger.WriteVerbose($"Finished task for machine: {machineName}");
                    } catch (Exception ex) {
                        exceptions.Enqueue(ex);
                    }
                });
            } catch (Exception ex) {
                exceptions.Enqueue(ex);
            } finally {
                results.CompleteAdding();
            }
        }, cancellationToken);

        return EnumerateResults(results, workerTask, exceptions, cancellationToken);
    }

    private static IEnumerable<EventObject> EnumerateResults(BlockingCollection<EventObject> results, Task workerTask, ConcurrentQueue<Exception> exceptions, CancellationToken cancellationToken) {
        try {
            foreach (var result in results.GetConsumingEnumerable(cancellationToken)) {
                yield return result;
            }
        } finally {
            try {
                workerTask.Wait(cancellationToken);
            } catch (Exception ex) {
                exceptions.Enqueue(ex);
            }

            if (!exceptions.IsEmpty) {
                throw new AggregateException(exceptions);
            }
        }
    }

    public static IEnumerable<EventObject> QueryLogsParallelForEach(KnownLog logName, List<int> eventIds = null, List<string> machineNames = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, int maxThreads = 4, List<long> eventRecordId = null, CancellationToken cancellationToken = default) {
        return QueryLogsParallelForEach(LogNameToString(logName), eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, cancellationToken);
    }
}
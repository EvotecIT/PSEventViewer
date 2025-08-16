using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    public static async IAsyncEnumerable<EventObject> QueryLogsParallel(string logName, List<int>? eventIds = null, List<string?>? machineNames = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, int maxThreads = 8, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        if (machineNames == null || !machineNames.Any()) {
            machineNames = new List<string?> { null };
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

    public static async Task<IEnumerable<EventObject>> QueryLogsParallelAsync(string logName, List<int>? eventIds = null, List<string?>? machineNames = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, int maxThreads = 8, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        var results = new List<EventObject>();
        await foreach (var ev in QueryLogsParallel(logName, eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, timePeriod, cancellationToken)) {
            results.Add(ev);
        }
        return results;
    }

    public static IAsyncEnumerable<EventObject> QueryLogsParallel(KnownLog logName, List<int>? eventIds = null, List<string?>? machineNames = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, int maxThreads = 8, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        return QueryLogsParallel(LogNameToString(logName), eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, timePeriod, cancellationToken);
    }

    public static Task<IEnumerable<EventObject>> QueryLogsParallelAsync(KnownLog logName, List<int>? eventIds = null, List<string?>? machineNames = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, int maxThreads = 8, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        return QueryLogsParallelAsync(LogNameToString(logName), eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, timePeriod, cancellationToken);
    }

    private static Task CreateTask(string? machineName, string logName, List<int>? eventIds, string? providerName, Keywords? keywords, Level? level, DateTime? startTime, DateTime? endTime, string? userId, int maxEvents, SemaphoreSlim semaphore, BlockingCollection<EventObject> results, CancellationToken cancellationToken, List<long>? eventRecordId = null, TimePeriod? timePeriod = null) {
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

    public static IEnumerable<EventObject> QueryLogsParallelForEach(string logName, List<int>? eventIds = null, List<string?>? machineNames = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, int maxThreads = 4, List<long>? eventRecordId = null, CancellationToken cancellationToken = default) {
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

    public static IEnumerable<EventObject> QueryLogsParallelForEach(KnownLog logName, List<int>? eventIds = null, List<string?>? machineNames = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, int maxThreads = 4, List<long>? eventRecordId = null, CancellationToken cancellationToken = default) {
        return QueryLogsParallelForEach(LogNameToString(logName), eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, cancellationToken);
    }
}

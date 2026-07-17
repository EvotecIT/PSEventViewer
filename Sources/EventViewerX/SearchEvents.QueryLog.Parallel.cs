using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    internal const int MaxXPathExpressionCount = 22;
    private const int MaxParallelism = 1024;

    /// <summary>
    /// Streams events from one or more machines through a bounded parallel query pipeline.
    /// </summary>
    /// <remarks>
    /// <paramref name="maxEvents"/> is a global limit across all machines and filter chunks.
    /// Result order is intentionally unspecified when more than one query runs concurrently.
    /// </remarks>
    public static async IAsyncEnumerable<EventObject> QueryLogsParallel(
        string logName,
        List<int>? eventIds = null,
        List<string?>? machineNames = null,
        string? providerName = null,
        Keywords? keywords = null,
        Level? level = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        string? userId = null,
        int maxEvents = 0,
        int maxThreads = 8,
        List<long>? eventRecordId = null,
        TimePeriod? timePeriod = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        int? sessionTimeoutMs = null,
        EventReadMode readMode = EventReadMode.Full,
        int bufferCapacity = 0) {

        ValidateParallelArguments(logName, maxEvents, maxThreads, bufferCapacity, sessionTimeoutMs);
        List<string?> targets = machineNames == null || machineNames.Count == 0
            ? new List<string?> { null }
            : machineNames;
        int fixedExpressionCount = CountFixedQueryExpressions(providerName, keywords, level, startTime, endTime, userId, timePeriod);
        int effectiveBufferCapacity = bufferCapacity > 0 ? bufferCapacity : Math.Max(16, maxThreads * 4);

        using var workerCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var results = new BlockingCollection<EventObject>(effectiveBufferCapacity);
        using IEnumerator<QueryWorkItem> workItems = BuildQueryWorkItems(targets, eventIds, eventRecordId, fixedExpressionCount).GetEnumerator();
        var workItemSync = new object();
        var failures = new ConcurrentQueue<Exception>();
        int produced = 0;
        int published = 0;

        var tasks = new List<Task>(maxThreads);
        for (int workerIndex = 0; workerIndex < maxThreads; workerIndex++) {
            tasks.Add(Task.Run(() => {
                try {
                    while (TryTakeWorkItem(workItems, workItemSync, workerCancellation.Token, out QueryWorkItem workItem)) {
                        foreach (EventObject result in QueryLogEnumerable(
                                     logName,
                                     workItem.EventIds,
                                     workItem.MachineName,
                                     providerName,
                                     keywords,
                                     level,
                                     startTime,
                                     endTime,
                                     userId,
                                     maxEvents: 0,
                                     eventRecordId: workItem.EventRecordIds,
                                     timePeriod: timePeriod,
                                     cancellationToken: workerCancellation.Token,
                                     sessionTimeoutMs: sessionTimeoutMs ?? Settings.QuerySessionTimeoutMs,
                                     readMode: readMode)) {
                            if (!TryReserveResult(ref produced, maxEvents)) {
                                return;
                            }

                            results.Add(result, workerCancellation.Token);
                            if (maxEvents > 0 && Interlocked.Increment(ref published) >= maxEvents) {
                                workerCancellation.Cancel();
                                return;
                            }
                        }

                        if (maxEvents > 0 && Volatile.Read(ref produced) >= maxEvents) {
                            break;
                        }
                    }
                } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && workerCancellation.IsCancellationRequested) {
                    // The shared result limit or another worker stopped remaining work.
                } catch (Exception ex) {
                    failures.Enqueue(ex);
                    workerCancellation.Cancel();
                }
            }, CancellationToken.None));
        }

        Task workers = Task.WhenAll(tasks);
        _ = workers.ContinueWith(
            _ => results.CompleteAdding(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        bool workersObserved = false;
        try {
            foreach (EventObject result in results.GetConsumingEnumerable(cancellationToken)) {
                yield return result;
                await Task.Yield();
            }

            await workers.ConfigureAwait(false);
            workersObserved = true;
            if (failures.TryDequeue(out Exception? failure)) {
                throw failure;
            }
        } finally {
            workerCancellation.Cancel();
            if (!workersObserved) {
                try {
                    await workers.ConfigureAwait(false);
                } catch when (!cancellationToken.IsCancellationRequested) {
                    // An early consumer stop owns cancellation; worker failures are surfaced during full enumeration.
                }
            }
        }
    }

    /// <summary>Materializes the bounded parallel query results.</summary>
    public static async Task<IEnumerable<EventObject>> QueryLogsParallelAsync(
        string logName,
        List<int>? eventIds = null,
        List<string?>? machineNames = null,
        string? providerName = null,
        Keywords? keywords = null,
        Level? level = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        string? userId = null,
        int maxEvents = 0,
        int maxThreads = 8,
        List<long>? eventRecordId = null,
        TimePeriod? timePeriod = null,
        CancellationToken cancellationToken = default,
        int? sessionTimeoutMs = null,
        EventReadMode readMode = EventReadMode.Full,
        int bufferCapacity = 0) {

        var results = new List<EventObject>();
        try {
            await foreach (EventObject ev in QueryLogsParallel(logName, eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, timePeriod, cancellationToken, sessionTimeoutMs, readMode, bufferCapacity)) {
                results.Add(ev);
            }
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw new OperationCanceledException(cancellationToken);
        }
        return results;
    }

    /// <summary>Streams events from a known log through the bounded parallel query pipeline.</summary>
    public static IAsyncEnumerable<EventObject> QueryLogsParallel(
        KnownLog logName,
        List<int>? eventIds = null,
        List<string?>? machineNames = null,
        string? providerName = null,
        Keywords? keywords = null,
        Level? level = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        string? userId = null,
        int maxEvents = 0,
        int maxThreads = 8,
        List<long>? eventRecordId = null,
        TimePeriod? timePeriod = null,
        CancellationToken cancellationToken = default,
        int? sessionTimeoutMs = null,
        EventReadMode readMode = EventReadMode.Full,
        int bufferCapacity = 0) {

        return QueryLogsParallel(LogNameToString(logName), eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, timePeriod, cancellationToken, sessionTimeoutMs, readMode, bufferCapacity);
    }

    /// <summary>Materializes events from a known log through the bounded parallel query pipeline.</summary>
    public static Task<IEnumerable<EventObject>> QueryLogsParallelAsync(
        KnownLog logName,
        List<int>? eventIds = null,
        List<string?>? machineNames = null,
        string? providerName = null,
        Keywords? keywords = null,
        Level? level = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        string? userId = null,
        int maxEvents = 0,
        int maxThreads = 8,
        List<long>? eventRecordId = null,
        TimePeriod? timePeriod = null,
        CancellationToken cancellationToken = default,
        int? sessionTimeoutMs = null,
        EventReadMode readMode = EventReadMode.Full,
        int bufferCapacity = 0) {

        return QueryLogsParallelAsync(LogNameToString(logName), eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, timePeriod, cancellationToken, sessionTimeoutMs, readMode, bufferCapacity);
    }

    /// <summary>
    /// Synchronous compatibility adapter over <see cref="QueryLogsParallel(string,List{int}?,List{string?}?,string?,Keywords?,Level?,DateTime?,DateTime?,string?,int,int,List{long}?,TimePeriod?,CancellationToken,int?,EventReadMode,int)"/>.
    /// </summary>
    [Obsolete("Use QueryLogsParallel for bounded asynchronous streaming.")]
    public static IEnumerable<EventObject> QueryLogsParallelForEach(
        string logName,
        List<int>? eventIds = null,
        List<string?>? machineNames = null,
        string? providerName = null,
        Keywords? keywords = null,
        Level? level = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        string? userId = null,
        int maxEvents = 0,
        int maxThreads = 8,
        List<long>? eventRecordId = null,
        CancellationToken cancellationToken = default,
        int? sessionTimeoutMs = null,
        EventReadMode readMode = EventReadMode.Full,
        int bufferCapacity = 0) {

        IAsyncEnumerator<EventObject> enumerator = QueryLogsParallel(logName, eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, cancellationToken: cancellationToken, sessionTimeoutMs: sessionTimeoutMs, readMode: readMode, bufferCapacity: bufferCapacity).GetAsyncEnumerator(cancellationToken);
        try {
            while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult()) {
                yield return enumerator.Current;
            }
        } finally {
            enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    /// <summary>Synchronous compatibility adapter for known log values.</summary>
    [Obsolete("Use QueryLogsParallel for bounded asynchronous streaming.")]
    public static IEnumerable<EventObject> QueryLogsParallelForEach(
        KnownLog logName,
        List<int>? eventIds = null,
        List<string?>? machineNames = null,
        string? providerName = null,
        Keywords? keywords = null,
        Level? level = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        string? userId = null,
        int maxEvents = 0,
        int maxThreads = 8,
        List<long>? eventRecordId = null,
        CancellationToken cancellationToken = default,
        int? sessionTimeoutMs = null,
        EventReadMode readMode = EventReadMode.Full,
        int bufferCapacity = 0) {

        return QueryLogsParallelForEach(LogNameToString(logName), eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, cancellationToken, sessionTimeoutMs, readMode, bufferCapacity);
    }

    private static bool TryReserveResult(ref int produced, int maxEvents) {
        while (true) {
            int current = Volatile.Read(ref produced);
            if (maxEvents > 0 && current >= maxEvents) {
                return false;
            }

            int next = current + 1;
            if (Interlocked.CompareExchange(ref produced, next, current) == current) {
                return true;
            }
        }
    }

    internal static IEnumerable<QueryWorkItem> BuildQueryWorkItems(List<string?> machineNames, List<int>? eventIds, List<long>? eventRecordIds, int fixedExpressionCount) {
        if (fixedExpressionCount < 0 || fixedExpressionCount >= MaxXPathExpressionCount) {
            throw new ArgumentOutOfRangeException(nameof(fixedExpressionCount), $"Fixed query expressions must be between zero and {MaxXPathExpressionCount - 1}.");
        }
        if (eventIds != null && eventIds.Any(static id => id <= 0)) {
            throw new ArgumentException("Event IDs must be positive.", nameof(eventIds));
        }
        if (eventRecordIds != null && eventRecordIds.Any(static id => id <= 0)) {
            throw new ArgumentException("Event record IDs must be positive.", nameof(eventRecordIds));
        }

        eventIds = eventIds?.Distinct().ToList();
        eventRecordIds = eventRecordIds?.Distinct().ToList();
        int availableExpressions = MaxXPathExpressionCount - fixedExpressionCount;
        (int eventIdChunkSize, int eventRecordIdChunkSize) = AllocateChunkSizes(eventIds?.Count ?? 0, eventRecordIds?.Count ?? 0, availableExpressions);
        foreach (string? machineName in machineNames) {
            foreach (List<int>? eventIdChunk in EnumerateChunks(eventIds, eventIdChunkSize)) {
                foreach (List<long>? eventRecordIdChunk in EnumerateChunks(eventRecordIds, eventRecordIdChunkSize)) {
                    yield return new QueryWorkItem(machineName, eventIdChunk, eventRecordIdChunk);
                }
            }
        }
    }

    private static (int EventIdChunkSize, int EventRecordIdChunkSize) AllocateChunkSizes(int eventIdCount, int eventRecordIdCount, int availableExpressions) {
        if (eventIdCount <= 0) {
            return (0, Math.Min(eventRecordIdCount, availableExpressions));
        }
        if (eventRecordIdCount <= 0) {
            return (Math.Min(eventIdCount, availableExpressions), 0);
        }
        if (availableExpressions < 2) {
            throw new ArgumentOutOfRangeException(nameof(availableExpressions), "At least two XPath expressions are required when both event IDs and event record IDs are supplied.");
        }

        int eventIdChunkSize = Math.Min(eventIdCount, Math.Max(1, availableExpressions / 2));
        int eventRecordIdChunkSize = Math.Min(eventRecordIdCount, Math.Max(1, availableExpressions - eventIdChunkSize));
        int unusedExpressions = availableExpressions - eventIdChunkSize - eventRecordIdChunkSize;
        if (unusedExpressions > 0) {
            int eventIdIncrease = Math.Min(unusedExpressions, eventIdCount - eventIdChunkSize);
            eventIdChunkSize += eventIdIncrease;
            unusedExpressions -= eventIdIncrease;
            eventRecordIdChunkSize += Math.Min(unusedExpressions, eventRecordIdCount - eventRecordIdChunkSize);
        }

        return (eventIdChunkSize, eventRecordIdChunkSize);
    }

    private static IEnumerable<List<T>?> EnumerateChunks<T>(List<T>? values, int chunkSize) {
        if (values == null || values.Count == 0) {
            yield return values;
            yield break;
        }

        if (chunkSize <= 0) {
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be positive when values are supplied.");
        }

        for (int offset = 0; offset < values.Count; offset += chunkSize) {
            int count = Math.Min(chunkSize, values.Count - offset);
            yield return values.GetRange(offset, count);
        }
    }

    private static bool TryTakeWorkItem(IEnumerator<QueryWorkItem> workItems, object syncRoot, CancellationToken cancellationToken, out QueryWorkItem workItem) {
        lock (syncRoot) {
            cancellationToken.ThrowIfCancellationRequested();
            if (workItems.MoveNext()) {
                workItem = workItems.Current;
                return true;
            }
        }

        workItem = null!;
        return false;
    }

    internal static int CountFixedQueryExpressions(string? providerName, Keywords? keywords, Level? level, DateTime? startTime, DateTime? endTime, string? userId, TimePeriod? timePeriod) {
        int count = 0;
        if (!string.IsNullOrEmpty(providerName)) {
            count++;
        }
        if (keywords.HasValue) {
            count++;
        }
        if (level.HasValue) {
            count++;
        }
        if (timePeriod.HasValue || startTime.HasValue || endTime.HasValue) {
            count++;
        }
        if (!string.IsNullOrEmpty(userId)) {
            count++;
        }
        return count;
    }

    private static void ValidateParallelArguments(string logName, int maxEvents, int maxThreads, int bufferCapacity, int? sessionTimeoutMs) {
        ValidateQueryArguments(logName, maxEvents, sessionTimeoutMs);
        if (maxThreads <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maxThreads), "Maximum threads must be positive.");
        }
        if (maxThreads > MaxParallelism) {
            throw new ArgumentOutOfRangeException(nameof(maxThreads), $"Maximum threads cannot exceed {MaxParallelism}.");
        }
        if (bufferCapacity < 0) {
            throw new ArgumentOutOfRangeException(nameof(bufferCapacity), "Buffer capacity must be greater than or equal to zero.");
        }
    }

    internal sealed class QueryWorkItem {
        internal QueryWorkItem(string? machineName, List<int>? eventIds, List<long>? eventRecordIds) {
            MachineName = machineName;
            EventIds = eventIds;
            EventRecordIds = eventRecordIds;
        }

        internal string? MachineName { get; }
        internal List<int>? EventIds { get; }
        internal List<long>? EventRecordIds { get; }
    }
}

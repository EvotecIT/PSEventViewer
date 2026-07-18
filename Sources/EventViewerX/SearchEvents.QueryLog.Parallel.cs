using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    internal const int MaxXPathExpressionCount = 22;
    /// <summary>Maximum supported query concurrency across the reusable event APIs.</summary>
    public const int MaximumParallelism = 64;

    /// <summary>
    /// Streams events from one or more machines through a bounded parallel query pipeline.
    /// </summary>
    /// <remarks>
    /// <paramref name="maxEvents"/> is a global limit across all machines and filter chunks.
    /// A positive limit uses a bounded global merge in the requested direction so producer arrival cannot change the selected set.
    /// Unlimited result order is intentionally unspecified when more than one query runs concurrently.
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
        int bufferCapacity = 0,
        Func<string?, long?>? minimumEventRecordIdExclusiveResolver = null,
        bool oldest = false) {

        if (oldest && minimumEventRecordIdExclusiveResolver != null) {
            ValidateParallelArguments(logName, maxEvents, maxThreads, bufferCapacity, sessionTimeoutMs);
            foreach (EventObject result in QueryLogsSequential(
                         logName: logName,
                         eventIds: eventIds,
                         machineNames: machineNames,
                         providerName: providerName,
                         keywords: keywords,
                         level: level,
                         startTime: startTime,
                         endTime: endTime,
                         userId: userId,
                         maxEvents: maxEvents,
                         eventRecordId: eventRecordId,
                         timePeriod: timePeriod,
                         cancellationToken: cancellationToken,
                         sessionTimeoutMs: sessionTimeoutMs,
                         readMode: readMode,
                         minimumEventRecordIdExclusiveResolver: minimumEventRecordIdExclusiveResolver,
                         maxOpenQueries: maxThreads,
                         oldest: true)) {
                yield return result;
            }
            yield break;
        }

        await foreach (EventObject result in QueryLogsParallelCore(
                           logName,
                           eventIds,
                           machineNames,
                           providerName,
                           keywords,
                           level,
                           startTime,
                           endTime,
                           userId,
                           maxEvents,
                           maxThreads,
                           eventRecordId,
                           timePeriod,
                           cancellationToken,
                           sessionTimeoutMs,
                           readMode,
                           bufferCapacity,
                           minimumEventRecordIdExclusiveResolver,
                           oldest,
                           resultPredicate: null,
                           candidateObserver: null)) {
            yield return result;
        }
    }

    private static async IAsyncEnumerable<EventObject> QueryLogsParallelCore(
        string logName,
        List<int>? eventIds,
        List<string?>? machineNames,
        string? providerName,
        Keywords? keywords,
        Level? level,
        DateTime? startTime,
        DateTime? endTime,
        string? userId,
        int maxEvents,
        int maxThreads,
        List<long>? eventRecordId,
        TimePeriod? timePeriod,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        int? sessionTimeoutMs,
        EventReadMode readMode,
        int bufferCapacity,
        Func<string?, long?>? minimumEventRecordIdExclusiveResolver,
        bool oldest,
        Func<EventObject, bool>? resultPredicate,
        Action<EventObject>? candidateObserver) {

        ValidateParallelArguments(logName, maxEvents, maxThreads, bufferCapacity, sessionTimeoutMs);
        List<string?> targets = NormalizeQueryTargets(machineNames);
        int fixedExpressionCount = CountFixedQueryExpressions(providerName, keywords, level, startTime, endTime, userId, timePeriod);
        bool isolateRemoteFailures = targets.Count > 1;
        int effectiveBufferCapacity = bufferCapacity > 0 ? bufferCapacity : Math.Max(16, maxThreads * 4);

        using var workerCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Channel<EventObject> results = Channel.CreateBounded<EventObject>(new BoundedChannelOptions(effectiveBufferCapacity) {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
        using IEnumerator<QueryWorkItem> workItems = BuildQueryWorkItems(
            targets,
            eventIds,
            eventRecordId,
            fixedExpressionCount,
            minimumEventRecordIdExclusiveResolver).GetEnumerator();
        var workItemSync = new object();
        var failures = new ConcurrentQueue<Exception>();
        var failedTargets = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        var tasks = new List<Task>(maxThreads);
        for (int workerIndex = 0; workerIndex < maxThreads; workerIndex++) {
            tasks.Add(Task.Run(() => {
                try {
                    while (TryTakeWorkItem(workItems, workItemSync, workerCancellation.Token, out QueryWorkItem workItem)) {
                        if (isolateRemoteFailures && ShouldSkipFailedTarget(workItem, failedTargets)) {
                            continue;
                        }

                        using IEnumerator<EventObject> queryResults = FilterQueryWorkItemResults(
                            workItem,
                            QueryLogEnumerable(
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
                                readMode: readMode,
                                minimumEventRecordIdExclusive: workItem.MinimumEventRecordIdExclusive,
                                maximumEventRecordIdExclusive: workItem.MaximumEventRecordIdExclusive,
                                oldest: oldest)).GetEnumerator();
                        int workItemResults = 0;
                        while (TryMoveNextParallelResult(queryResults, workItem, failedTargets, isolateRemoteFailures, out EventObject? result)) {
                            candidateObserver?.Invoke(result!);
                            if (resultPredicate != null && !resultPredicate(result!)) {
                                continue;
                            }
                            if (!results.Writer.TryWrite(result!)) {
                                results.Writer.WriteAsync(result!, workerCancellation.Token).AsTask().GetAwaiter().GetResult();
                            }
                            workItemResults++;
                            if (maxEvents > 0 && workItemResults >= maxEvents) {
                                break;
                            }
                        }
                    }
                } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && workerCancellation.IsCancellationRequested) {
                    // Another worker failed or the consumer stopped before full enumeration.
                } catch (Exception ex) {
                    failures.Enqueue(ex);
                    workerCancellation.Cancel();
                }
            }, CancellationToken.None));
        }

        Task workers = Task.WhenAll(tasks);
        _ = workers.ContinueWith(
            _ => results.Writer.TryComplete(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        bool workersObserved = false;
        List<EventObject>? candidates = maxEvents > 0
            ? new List<EventObject>(Math.Min(maxEvents, 256))
            : null;
        try {
            while (await results.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (results.Reader.TryRead(out EventObject? result)) {
                    if (candidates == null) {
                        yield return result!;
                        continue;
                    }

                    candidates.Add(result!);
                    long trimThreshold = Math.Min((long)maxEvents * 2, (long)maxEvents + 1024);
                    if (candidates.Count >= trimThreshold) {
                        SortAndTrim(candidates, maxEvents, oldest);
                    }
                }
            }

            await workers.ConfigureAwait(false);
            workersObserved = true;
            if (failures.TryDequeue(out Exception? failure)) {
                throw failure;
            }

            if (candidates != null) {
                candidates.Sort((left, right) => CompareEvents(left, right, oldest));
                int count = Math.Min(maxEvents, candidates.Count);
                for (int index = 0; index < count; index++) {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return candidates[index];
                }
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

    internal static IAsyncEnumerable<EventObject> QueryLogsParallelMatching(
        string logName,
        List<int>? eventIds,
        List<string?>? machineNames,
        DateTime? startTime,
        DateTime? endTime,
        TimePeriod? timePeriod,
        int maxEvents,
        int maxThreads,
        CancellationToken cancellationToken,
        Func<EventObject, bool> resultPredicate,
        Action<EventObject>? candidateObserver = null,
        Func<string?, long?>? minimumEventRecordIdExclusiveResolver = null,
        bool oldest = false) {

        if (resultPredicate == null) {
            throw new ArgumentNullException(nameof(resultPredicate));
        }

        return QueryLogsParallelCore(
            logName: logName,
            eventIds: eventIds,
            machineNames: machineNames,
            providerName: null,
            keywords: null,
            level: null,
            startTime: startTime,
            endTime: endTime,
            userId: null,
            maxEvents: maxEvents,
            maxThreads: maxThreads,
            eventRecordId: null,
            timePeriod: timePeriod,
            cancellationToken: cancellationToken,
            sessionTimeoutMs: null,
            readMode: EventReadMode.Full,
            bufferCapacity: 0,
            minimumEventRecordIdExclusiveResolver: minimumEventRecordIdExclusiveResolver,
            oldest: oldest,
            resultPredicate: resultPredicate,
            candidateObserver: candidateObserver);
    }

    internal static bool ShouldSkipFailedTarget(
        QueryWorkItem workItem,
        ConcurrentDictionary<string, byte> failedTargets) {

        string? target = NormalizeRemoteTarget(workItem.MachineName);
        return target != null && failedTargets.ContainsKey(target);
    }

    private static bool TryMoveNextParallelResult(
        IEnumerator<EventObject> queryResults,
        QueryWorkItem workItem,
        ConcurrentDictionary<string, byte> failedTargets,
        bool isolateRemoteFailures,
        out EventObject? result) {

        if (isolateRemoteFailures) {
            return TryMoveNextQueryWorkItem(queryResults, workItem, failedTargets, out result);
        }

        if (!queryResults.MoveNext()) {
            result = null;
            return false;
        }

        result = queryResults.Current;
        return true;
    }

    internal static bool TryMoveNextQueryWorkItem(
        IEnumerator<EventObject> queryResults,
        QueryWorkItem workItem,
        ConcurrentDictionary<string, byte> failedTargets,
        out EventObject? result) {

        try {
            if (!queryResults.MoveNext()) {
                result = null;
                return false;
            }

            result = queryResults.Current;
            return true;
        } catch (Exception ex) when (EventLogRemoteQueryFailureClassifier.TryClassify(workItem.MachineName, ex, out _)) {
            string target = NormalizeRemoteTarget(workItem.MachineName)!;
            if (failedTargets.TryAdd(target, 0)) {
                _logger.WriteWarning($"Skipping event-log target '{target}' after {ex.GetType().Name}: {ex.Message}");
            }
            result = null;
            return false;
        }
    }

    private static string? NormalizeRemoteTarget(string? machineName) {
        return string.IsNullOrWhiteSpace(machineName) ? null : machineName!.Trim();
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
        int bufferCapacity = 0,
        Func<string?, long?>? minimumEventRecordIdExclusiveResolver = null,
        bool oldest = false) {

        var results = new List<EventObject>();
        try {
            await foreach (EventObject ev in QueryLogsParallel(logName, eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, timePeriod, cancellationToken, sessionTimeoutMs, readMode, bufferCapacity, minimumEventRecordIdExclusiveResolver, oldest)) {
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
        int bufferCapacity = 0,
        Func<string?, long?>? minimumEventRecordIdExclusiveResolver = null,
        bool oldest = false) {

        return QueryLogsParallel(LogNameToString(logName), eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, timePeriod, cancellationToken, sessionTimeoutMs, readMode, bufferCapacity, minimumEventRecordIdExclusiveResolver, oldest);
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
        int bufferCapacity = 0,
        Func<string?, long?>? minimumEventRecordIdExclusiveResolver = null,
        bool oldest = false) {

        return QueryLogsParallelAsync(LogNameToString(logName), eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, timePeriod, cancellationToken, sessionTimeoutMs, readMode, bufferCapacity, minimumEventRecordIdExclusiveResolver, oldest);
    }

    /// <summary>
    /// Synchronous compatibility adapter over the bounded asynchronous <c>QueryLogsParallel</c> API.
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
        int bufferCapacity = 0,
        Func<string?, long?>? minimumEventRecordIdExclusiveResolver = null,
        bool oldest = false) {

        IAsyncEnumerator<EventObject> enumerator = QueryLogsParallel(logName, eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, cancellationToken: cancellationToken, sessionTimeoutMs: sessionTimeoutMs, readMode: readMode, bufferCapacity: bufferCapacity, minimumEventRecordIdExclusiveResolver: minimumEventRecordIdExclusiveResolver, oldest: oldest).GetAsyncEnumerator(cancellationToken);
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
        int bufferCapacity = 0,
        Func<string?, long?>? minimumEventRecordIdExclusiveResolver = null,
        bool oldest = false) {

        return QueryLogsParallelForEach(LogNameToString(logName), eventIds, machineNames, providerName, keywords, level, startTime, endTime, userId, maxEvents, maxThreads, eventRecordId, cancellationToken, sessionTimeoutMs, readMode, bufferCapacity, minimumEventRecordIdExclusiveResolver, oldest);
    }

    internal static IEnumerable<QueryWorkItem> BuildQueryWorkItems(
        List<string?> machineNames,
        List<int>? eventIds,
        List<long>? eventRecordIds,
        int fixedExpressionCount,
        Func<string?, long?>? minimumEventRecordIdExclusiveResolver = null,
        bool reserveRecordIdPagingBoundary = false) {

        int effectiveFixedExpressionCount = fixedExpressionCount +
                                            (minimumEventRecordIdExclusiveResolver != null ? 1 : 0) +
                                            (reserveRecordIdPagingBoundary ? 1 : 0);
        if (effectiveFixedExpressionCount < 0 || effectiveFixedExpressionCount > MaxXPathExpressionCount) {
            throw new ArgumentOutOfRangeException(nameof(fixedExpressionCount), $"Fixed query expressions must be between zero and {MaxXPathExpressionCount}.");
        }
        if (eventIds != null && eventIds.Any(static id => id <= 0)) {
            throw new ArgumentException("Event IDs must be positive.", nameof(eventIds));
        }
        if (eventRecordIds != null && eventRecordIds.Any(static id => id <= 0)) {
            throw new ArgumentException("Event record IDs must be positive.", nameof(eventRecordIds));
        }

        eventIds = eventIds?.Distinct().ToList();
        eventRecordIds = eventRecordIds?.Distinct().ToList();
        int availableExpressions = MaxXPathExpressionCount - effectiveFixedExpressionCount;
        bool hasEventIds = eventIds?.Count > 0;
        bool hasEventRecordIds = eventRecordIds?.Count > 0;
        if (availableExpressions == 0 && (hasEventIds || hasEventRecordIds)) {
            throw new ArgumentException($"The fixed filter consumes all {MaxXPathExpressionCount} XPath expressions; no capacity remains for event or record IDs.");
        }

        // Record IDs are exact and normally the more selective native filter. Keep event IDs managed
        // when both dimensions are supplied so large lists produce O(record chunks), not a chunk cross-product.
        HashSet<int>? managedEventIds = hasEventIds && hasEventRecordIds
            ? new HashSet<int>(eventIds!)
            : null;
        List<int>? nativeEventIds = managedEventIds == null ? eventIds : null;
        int eventIdChunkSize = hasEventIds && !hasEventRecordIds
            ? Math.Min(eventIds!.Count, availableExpressions)
            : 0;
        int eventRecordIdChunkSize = hasEventRecordIds
            ? Math.Min(eventRecordIds!.Count, availableExpressions)
            : 0;

        foreach (string? machineName in machineNames) {
            long? minimumEventRecordIdExclusive = minimumEventRecordIdExclusiveResolver?.Invoke(machineName);
            if (minimumEventRecordIdExclusive < 0) {
                throw new ArgumentOutOfRangeException(nameof(minimumEventRecordIdExclusiveResolver), "Minimum event record ID must be greater than or equal to zero.");
            }
            foreach (List<int>? eventIdChunk in EnumerateChunks(nativeEventIds, eventIdChunkSize)) {
                foreach (List<long>? eventRecordIdChunk in EnumerateChunks(eventRecordIds, eventRecordIdChunkSize)) {
                    yield return new QueryWorkItem(machineName, eventIdChunk, eventRecordIdChunk, managedEventIds, null, minimumEventRecordIdExclusive);
                }
            }
        }
    }

    private static IEnumerable<EventObject> FilterQueryWorkItemResults(QueryWorkItem workItem, IEnumerable<EventObject> events) {
        foreach (EventObject eventObject in events) {
            bool eventIdMatches = workItem.ManagedEventIds == null || workItem.ManagedEventIds.Contains(eventObject.Id);
            bool recordIdMatches = workItem.ManagedEventRecordIds == null ||
                                   (eventObject.RecordId.HasValue && workItem.ManagedEventRecordIds.Contains(eventObject.RecordId.Value));
            if (eventIdMatches && recordIdMatches) {
                yield return eventObject;
            }
        }
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

    internal static List<string?> NormalizeQueryTargets(List<string?>? machineNames) {
        return NormalizeMachineTargets(machineNames).ToList();
    }

    /// <summary>Trims and case-insensitively deduplicates event-log target names while preserving order.</summary>
    /// <param name="machineNames">Machine names to normalize; null or empty selects the local machine.</param>
    /// <returns>Normalized targets, using <c>null</c> for the local machine.</returns>
    public static IReadOnlyList<string?> NormalizeMachineTargets(IEnumerable<string?>? machineNames) {
        List<string?>? supplied = machineNames?.ToList();
        if (supplied == null || supplied.Count == 0) {
            return new List<string?> { null };
        }

        var targets = new List<string?>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string? machineName in supplied) {
            string? normalized = string.IsNullOrWhiteSpace(machineName) ? null : machineName!.Trim();
            string key = normalized ?? "<LOCAL>";
            if (seen.Add(key)) {
                targets.Add(normalized);
            }
        }
        return targets;
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

    internal static int CountFixedQueryExpressions(
        string? providerName,
        Keywords? keywords,
        Level? level,
        DateTime? startTime,
        DateTime? endTime,
        string? userId,
        TimePeriod? timePeriod,
        System.Collections.Hashtable? namedDataFilter = null,
        System.Collections.Hashtable? namedDataExcludeFilter = null) {
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
        if (timePeriod.HasValue) {
            count += 2;
        } else {
            count += startTime.HasValue ? 1 : 0;
            count += endTime.HasValue ? 1 : 0;
        }
        if (!string.IsNullOrEmpty(userId)) {
            count++;
        }
        count += CountNamedDataExpressions(namedDataFilter == null ? null : new[] { namedDataFilter });
        count += CountNamedDataExpressions(namedDataExcludeFilter == null ? null : new[] { namedDataExcludeFilter });
        return count;
    }

    private static void ValidateParallelArguments(string logName, int maxEvents, int maxThreads, int bufferCapacity, int? sessionTimeoutMs) {
        ValidateQueryArguments(logName, maxEvents, sessionTimeoutMs);
        if (maxThreads <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maxThreads), "Maximum threads must be positive.");
        }
        if (maxThreads > MaximumParallelism) {
            throw new ArgumentOutOfRangeException(nameof(maxThreads), $"Maximum threads cannot exceed {MaximumParallelism}.");
        }
        if (bufferCapacity < 0) {
            throw new ArgumentOutOfRangeException(nameof(bufferCapacity), "Buffer capacity must be greater than or equal to zero.");
        }
    }

    internal sealed class QueryWorkItem {
        internal QueryWorkItem(
            string? machineName,
            List<int>? eventIds,
            List<long>? eventRecordIds,
            HashSet<int>? managedEventIds = null,
            HashSet<long>? managedEventRecordIds = null,
            long? minimumEventRecordIdExclusive = null,
            long? maximumEventRecordIdExclusive = null) {
            MachineName = machineName;
            EventIds = eventIds;
            EventRecordIds = eventRecordIds;
            ManagedEventIds = managedEventIds;
            ManagedEventRecordIds = managedEventRecordIds;
            MinimumEventRecordIdExclusive = minimumEventRecordIdExclusive;
            MaximumEventRecordIdExclusive = maximumEventRecordIdExclusive;
        }

        internal string? MachineName { get; }
        internal List<int>? EventIds { get; }
        internal List<long>? EventRecordIds { get; }
        internal HashSet<int>? ManagedEventIds { get; }
        internal HashSet<long>? ManagedEventRecordIds { get; }
        internal long? MinimumEventRecordIdExclusive { get; }
        internal long? MaximumEventRecordIdExclusive { get; }
    }
}

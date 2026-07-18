using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    private const int CheckpointCandidateBufferBudget = 4096;
    private const int MaximumCheckpointCandidatePageSize = 1024;

    private static async IAsyncEnumerable<EventObject> QueryNamedPagedCandidatesAsync(
        Dictionary<string, HashSet<int>> eventInfo,
        List<string?>? machineNames,
        DateTime? startTime,
        DateTime? endTime,
        TimePeriod? timePeriod,
        int maxEvents,
        int maxThreads,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        Func<string?, string, long?>? minimumEventRecordIdExclusiveResolver,
        bool oldest,
        Action<EventLogQueryTargetFailure>? targetFailureObserver) {

        List<string?> targets = NormalizeNamedCheckpointTargets(machineNames);
        var pageReaders = new List<Func<int, IReadOnlyList<EventObject>>>(eventInfo.Count * targets.Count);
        int expectedSourceCount = eventInfo.Count * targets.Count;
        int boundedPageSize = maxEvents > 0 && expectedSourceCount > 0
            ? GetBoundedCandidatePageSize(expectedSourceCount, maxEvents)
            : 0;
        foreach (KeyValuePair<string, HashSet<int>> entry in eventInfo) {
            string logName = entry.Key;
            Func<string?, long?>? minimumResolver = minimumEventRecordIdExclusiveResolver == null
                ? null
                : machineName => minimumEventRecordIdExclusiveResolver(machineName, logName);
            int fixedExpressionCount = CountFixedQueryExpressions(
                providerName: null,
                keywords: null,
                level: null,
                startTime,
                endTime,
                userId: null,
                timePeriod);
            List<QueryWorkItem> workItems = BuildQueryWorkItems(
                    targets,
                    entry.Value.ToList(),
                    eventRecordIds: null,
                    fixedExpressionCount,
                    minimumResolver,
                    reserveRecordIdPagingBoundary: maxEvents > 0 && !(oldest && minimumResolver != null))
                .ToList();
            bool isolateRemoteFailures = targets.Count > 1;
            var failedTargets = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            Func<QueryWorkItem, IEnumerator<EventObject>> createEnumerator = workItem => CreateSequentialQueryEnumerator(
                workItem,
                logName,
                providerName: null,
                keywords: null,
                level: null,
                startTime,
                endTime,
                userId: null,
                timePeriod,
                cancellationToken,
                sessionTimeoutMs: null,
                EventReadMode.Full,
                oldest,
                isolateRemoteFailures,
                failedTargets,
                resultPredicate: null,
                candidateObserver: null,
                targetFailureObserver: targetFailureObserver);
            pageReaders.AddRange(CreateRecordOrderedSourcePageReaders(
                workItems,
                createEnumerator,
                oldest,
                cancellationToken,
                boundedPageSize));
        }

        if (pageReaders.Count == 0) {
            yield break;
        }

        int pageSize = maxEvents > 0
            ? boundedPageSize
            : GetCheckpointCandidatePageSize(pageReaders.Count);
        await foreach (EventObject eventObject in MergePagedSourcesParallel(
                           pageReaders,
                           (left, right) => CompareEvents(left, right, oldest),
                           pageSize,
                           maxThreads,
                           cancellationToken)) {
            yield return eventObject;
        }
    }

    internal static IEnumerable<T> MergePagedSources<T>(
        IEnumerable<Func<int, IReadOnlyList<T>>> sourcePageReaders,
        Comparison<T> comparison,
        int pageSize,
        CancellationToken cancellationToken = default) {

        if (sourcePageReaders == null) {
            throw new ArgumentNullException(nameof(sourcePageReaders));
        }
        if (comparison == null) {
            throw new ArgumentNullException(nameof(comparison));
        }
        if (pageSize <= 0) {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be positive.");
        }

        var queue = new SortedSet<PagedSourceCursor<T>>(new PagedSourceCursorComparer<T>(comparison));
        int index = 0;
        foreach (Func<int, IReadOnlyList<T>> pageReader in sourcePageReaders) {
            cancellationToken.ThrowIfCancellationRequested();
            var cursor = new PagedSourceCursor<T>(index++, pageReader);
            if (cursor.MoveNext(requested: 1)) {
                queue.Add(cursor);
            }
        }

        while (queue.Count > 0) {
            cancellationToken.ThrowIfCancellationRequested();
            PagedSourceCursor<T> cursor = queue.Min!;
            queue.Remove(cursor);
            yield return cursor.Current;
            if (cursor.MoveNext(pageSize)) {
                queue.Add(cursor);
            }
        }
    }

    internal static async IAsyncEnumerable<T> MergePagedSourcesParallel<T>(
        IReadOnlyList<Func<int, IReadOnlyList<T>>> sourcePageReaders,
        Comparison<T> comparison,
        int pageSize,
        int maxConcurrency,
        [EnumeratorCancellation] CancellationToken cancellationToken = default) {

        if (sourcePageReaders == null) {
            throw new ArgumentNullException(nameof(sourcePageReaders));
        }
        if (comparison == null) {
            throw new ArgumentNullException(nameof(comparison));
        }
        if (pageSize <= 0) {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be positive.");
        }
        if (maxConcurrency <= 0 || maxConcurrency > MaximumParallelism) {
            throw new ArgumentOutOfRangeException(
                nameof(maxConcurrency),
                $"Maximum concurrency must be between 1 and {MaximumParallelism}.");
        }
        if (sourcePageReaders.Count == 0) {
            yield break;
        }

        using var concurrencyGate = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var primeTasks = new Task<PagedSourceCursor<T>?>[sourcePageReaders.Count];
        for (int index = 0; index < sourcePageReaders.Count; index++) {
            primeTasks[index] = PrimePagedSourceCursorAsync(
                index,
                sourcePageReaders[index],
                concurrencyGate,
                cancellationToken);
        }

        PagedSourceCursor<T>?[] primedCursors = await Task.WhenAll(primeTasks).ConfigureAwait(false);
        var queue = new SortedSet<PagedSourceCursor<T>>(new PagedSourceCursorComparer<T>(comparison));
        foreach (PagedSourceCursor<T>? cursor in primedCursors) {
            if (cursor != null) {
                queue.Add(cursor);
            }
        }

        while (queue.Count > 0) {
            cancellationToken.ThrowIfCancellationRequested();
            PagedSourceCursor<T> cursor = queue.Min!;
            queue.Remove(cursor);
            yield return cursor.Current;
            bool hasNext = cursor.NeedsPage
                ? await Task.Run(() => cursor.MoveNext(pageSize), cancellationToken).ConfigureAwait(false)
                : cursor.MoveNext(pageSize);
            if (hasNext) {
                queue.Add(cursor);
            }
        }
    }

    private static async Task<PagedSourceCursor<T>?> PrimePagedSourceCursorAsync<T>(
        int index,
        Func<int, IReadOnlyList<T>> pageReader,
        SemaphoreSlim concurrencyGate,
        CancellationToken cancellationToken) {

        await concurrencyGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            var cursor = new PagedSourceCursor<T>(index, pageReader);
            bool hasCurrent = await Task.Run(() => cursor.MoveNext(requested: 1), cancellationToken).ConfigureAwait(false);
            return hasCurrent ? cursor : null;
        } finally {
            concurrencyGate.Release();
        }
    }

    private static List<string?> NormalizeNamedCheckpointTargets(List<string?>? machineNames)
        => NormalizeQueryTargets(machineNames);

    internal static int GetCheckpointCandidatePageSize(int sourceCount) {
        if (sourceCount <= 0) {
            throw new ArgumentOutOfRangeException(nameof(sourceCount), "Source count must be positive.");
        }

        return Math.Min(
            MaximumCheckpointCandidatePageSize,
            Math.Max(1, CheckpointCandidateBufferBudget / sourceCount));
    }

    internal static int GetBoundedCandidatePageSize(int sourceCount, int maxEvents) {
        if (maxEvents <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maxEvents), "Maximum events must be positive.");
        }

        return Math.Min(
            GetCheckpointCandidatePageSize(sourceCount),
            Math.Max(1, maxEvents / sourceCount));
    }

    internal static int CompareCheckpointEvents(EventObject left, EventObject right) {
        return CompareEvents(left, right, oldest: true);
    }

    internal static int CompareRecordOrderedEvents(EventObject left, EventObject right, bool oldest) {
        if (left == null) {
            throw new ArgumentNullException(nameof(left));
        }
        if (right == null) {
            throw new ArgumentNullException(nameof(right));
        }

        if (left.RecordId.HasValue && right.RecordId.HasValue) {
            int recordComparison = oldest
                ? left.RecordId.Value.CompareTo(right.RecordId.Value)
                : right.RecordId.Value.CompareTo(left.RecordId.Value);
            if (recordComparison != 0) {
                return recordComparison;
            }
        }

        return CompareEvents(left, right, oldest);
    }

    internal static List<Func<int, IReadOnlyList<EventObject>>> CreateRecordOrderedSourcePageReaders(
        IReadOnlyList<QueryWorkItem> workItems,
        Func<QueryWorkItem, IEnumerator<EventObject>> createEnumerator,
        bool oldest,
        CancellationToken cancellationToken = default,
        int boundedPageSize = 0) {

        if (workItems == null) {
            throw new ArgumentNullException(nameof(workItems));
        }
        if (createEnumerator == null) {
            throw new ArgumentNullException(nameof(createEnumerator));
        }
        if (boundedPageSize < 0) {
            throw new ArgumentOutOfRangeException(nameof(boundedPageSize), "Bounded page size cannot be negative.");
        }

        var sourceGroups = new List<List<QueryWorkItem>>();
        var sourceIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (QueryWorkItem workItem in workItems) {
            string sourceKey = workItem.MachineName == null ? "0:" : "1:" + workItem.MachineName;
            if (!sourceIndexes.TryGetValue(sourceKey, out int sourceIndex)) {
                sourceIndex = sourceGroups.Count;
                sourceIndexes.Add(sourceKey, sourceIndex);
                sourceGroups.Add(new List<QueryWorkItem>());
            }
            sourceGroups[sourceIndex].Add(workItem);
        }

        var sourcePageReaders = new List<Func<int, IReadOnlyList<EventObject>>>(sourceGroups.Count);
        foreach (List<QueryWorkItem> sourceGroup in sourceGroups) {
            List<Func<int, IReadOnlyList<EventObject>>> chunkPageReaders = sourceGroup
                .Select(workItem => CreateCheckpointPageReader(workItem, createEnumerator, oldest))
                .ToList();
            if (chunkPageReaders.Count == 1) {
                sourcePageReaders.Add(chunkPageReaders[0]);
                continue;
            }

            IEnumerator<EventObject>? mergedChunks = null;
            bool completed = false;
            int chunkPageSize = boundedPageSize > 0
                ? Math.Min(GetCheckpointCandidatePageSize(chunkPageReaders.Count), boundedPageSize)
                : GetCheckpointCandidatePageSize(chunkPageReaders.Count);
            sourcePageReaders.Add(requested => {
                if (requested <= 0) {
                    throw new ArgumentOutOfRangeException(nameof(requested), "Requested page size must be positive.");
                }
                if (completed) {
                    return Array.Empty<EventObject>();
                }

                mergedChunks ??= MergePagedSources(
                        chunkPageReaders,
                        (left, right) => CompareRecordOrderedEvents(left, right, oldest),
                        chunkPageSize,
                        cancellationToken)
                    .GetEnumerator();
                var page = new List<EventObject>(requested);
                while (page.Count < requested && mergedChunks.MoveNext()) {
                    page.Add(mergedChunks.Current);
                }
                if (page.Count < requested) {
                    completed = true;
                    mergedChunks.Dispose();
                    mergedChunks = null;
                }
                return page;
            });
        }
        return sourcePageReaders;
    }

    internal static Func<int, IReadOnlyList<EventObject>> CreateCheckpointPageReader(
        QueryWorkItem initialWorkItem,
        Func<QueryWorkItem, IEnumerator<EventObject>> createEnumerator,
        bool oldest = true) {

        if (initialWorkItem == null) {
            throw new ArgumentNullException(nameof(initialWorkItem));
        }
        if (createEnumerator == null) {
            throw new ArgumentNullException(nameof(createEnumerator));
        }

        long? lowerBound = initialWorkItem.MinimumEventRecordIdExclusive;
        long? upperBound = initialWorkItem.MaximumEventRecordIdExclusive;
        bool completed = false;
        return requested => {
            if (completed) {
                return Array.Empty<EventObject>();
            }

            var pageWorkItem = new QueryWorkItem(
                initialWorkItem.MachineName,
                initialWorkItem.EventIds,
                initialWorkItem.EventRecordIds,
                initialWorkItem.ManagedEventIds,
                initialWorkItem.ManagedEventRecordIds,
                lowerBound,
                upperBound);
            var page = new List<EventObject>(requested);
            using (IEnumerator<EventObject> enumerator = createEnumerator(pageWorkItem)) {
                while (page.Count < requested && enumerator.MoveNext()) {
                    page.Add(enumerator.Current);
                }
            }

            long? nextBoundary = null;
            foreach (EventObject eventObject in page) {
                if (eventObject.RecordId.HasValue &&
                    (!nextBoundary.HasValue ||
                     (oldest
                         ? eventObject.RecordId.Value > nextBoundary.Value
                         : eventObject.RecordId.Value < nextBoundary.Value))) {
                    nextBoundary = eventObject.RecordId.Value;
                }
            }
            if (page.Count < requested ||
                !nextBoundary.HasValue ||
                (oldest && lowerBound.HasValue && nextBoundary.Value <= lowerBound.Value) ||
                (!oldest && upperBound.HasValue && nextBoundary.Value >= upperBound.Value)) {
                completed = true;
            } else if (oldest) {
                lowerBound = nextBoundary;
            } else {
                upperBound = nextBoundary;
            }
            return page;
        };
    }

    private sealed class PagedSourceCursor<T> {
        private readonly Func<int, IReadOnlyList<T>> _pageReader;
        private readonly Queue<T> _buffer = new();
        private bool _exhausted;

        internal PagedSourceCursor(int index, Func<int, IReadOnlyList<T>> pageReader) {
            Index = index;
            _pageReader = pageReader ?? throw new ArgumentNullException(nameof(pageReader));
            Current = default!;
        }

        internal int Index { get; }
        internal T Current { get; private set; }
        internal bool NeedsPage => _buffer.Count == 0 && !_exhausted;

        internal bool MoveNext(int requested) {
            if (_buffer.Count == 0 && !_exhausted) {
                IReadOnlyList<T> page = _pageReader(requested)
                    ?? throw new InvalidOperationException("A paged source returned a null page.");
                if (page.Count > requested) {
                    throw new InvalidOperationException("A paged source returned more items than requested.");
                }
                foreach (T item in page) {
                    _buffer.Enqueue(item);
                }
                if (page.Count < requested) {
                    _exhausted = true;
                }
            }

            if (_buffer.Count == 0) {
                Current = default!;
                return false;
            }

            Current = _buffer.Dequeue();
            return true;
        }
    }

    private sealed class PagedSourceCursorComparer<T> : IComparer<PagedSourceCursor<T>> {
        private readonly Comparison<T> _comparison;

        internal PagedSourceCursorComparer(Comparison<T> comparison) {
            _comparison = comparison;
        }

        public int Compare(PagedSourceCursor<T>? left, PagedSourceCursor<T>? right) {
            if (ReferenceEquals(left, right)) {
                return 0;
            }
            if (left == null) {
                return 1;
            }
            if (right == null) {
                return -1;
            }

            int result = _comparison(left.Current, right.Current);
            return result != 0 ? result : left.Index.CompareTo(right.Index);
        }
    }
}

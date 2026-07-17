using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace EventViewerX;

public partial class SearchEvents {
    internal const int MaxSequentialOpenQueries = 8;

    /// <summary>
    /// Streams events from one or more machines in target order using one global result limit.
    /// </summary>
    /// <remarks>
    /// Unlimited queries that require multiple XPath chunks stream bounded chunk batches; ordering is preserved
    /// within each batch. A positive <paramref name="maxEvents"/> keeps a bounded global newest-first merge.
    /// </remarks>
    public static IEnumerable<EventObject> QueryLogsSequential(
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
        List<long>? eventRecordId = null,
        TimePeriod? timePeriod = null,
        CancellationToken cancellationToken = default,
        int? sessionTimeoutMs = null,
        EventReadMode readMode = EventReadMode.Full,
        Func<string?, long?>? minimumEventRecordIdExclusiveResolver = null) {

        ValidateQueryArguments(logName, maxEvents, sessionTimeoutMs);
        List<string?> targets = machineNames == null || machineNames.Count == 0
            ? new List<string?> { null }
            : machineNames;
        int fixedExpressionCount = CountFixedQueryExpressions(providerName, keywords, level, startTime, endTime, userId, timePeriod);
        int returned = 0;
        var failedTargets = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        foreach (string? machineName in targets) {
            if (maxEvents > 0 && returned >= maxEvents) {
                yield break;
            }

            IEnumerable<QueryWorkItem> workItems = BuildQueryWorkItems(new List<string?> { machineName }, eventIds, eventRecordId, fixedExpressionCount, minimumEventRecordIdExclusiveResolver);
            int remaining = maxEvents > 0 ? maxEvents - returned : 0;
            foreach (EventObject result in MergeQueryWorkItems(
                         workItems,
                         workItem => ShouldSkipFailedTarget(workItem, failedTargets)
                             ? System.Linq.Enumerable.Empty<EventObject>().GetEnumerator()
                             : EnumerateQueryWorkItemSafely(
                                 workItem,
                                 FilterQueryWorkItemResults(
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
                                         cancellationToken: cancellationToken,
                                         sessionTimeoutMs: sessionTimeoutMs ?? Settings.QuerySessionTimeoutMs,
                                         readMode: readMode,
                                         minimumEventRecordIdExclusive: workItem.MinimumEventRecordIdExclusive)).GetEnumerator(),
                                 failedTargets).GetEnumerator(),
                         remaining,
                         oldest: false,
                         cancellationToken,
                         MaxSequentialOpenQueries)) {
                yield return result;
                returned++;
                if (maxEvents > 0 && returned >= maxEvents) {
                    yield break;
                }
            }
        }
    }

    private static IEnumerable<EventObject> EnumerateQueryWorkItemSafely(
        QueryWorkItem workItem,
        IEnumerator<EventObject> queryResults,
        ConcurrentDictionary<string, byte> failedTargets) {

        using (queryResults) {
            while (TryMoveNextQueryWorkItem(queryResults, workItem, failedTargets, out EventObject? result)) {
                yield return result!;
            }
        }
    }

    internal static IEnumerable<EventObject> MergeQueryWorkItems(
        IEnumerable<QueryWorkItem> workItems,
        Func<QueryWorkItem, IEnumerator<EventObject>> createEnumerator,
        int maxEvents,
        bool oldest,
        CancellationToken cancellationToken,
        int maxOpenQueries) {

        if (maxOpenQueries <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maxOpenQueries));
        }

        using IEnumerator<QueryWorkItem> source = workItems.GetEnumerator();
        if (!source.MoveNext()) {
            yield break;
        }

        QueryWorkItem first = source.Current;
        if (!source.MoveNext()) {
            using IEnumerator<EventObject> single = createEnumerator(first);
            int returned = 0;
            while (single.MoveNext()) {
                cancellationToken.ThrowIfCancellationRequested();
                yield return single.Current;
                returned++;
                if (maxEvents > 0 && returned >= maxEvents) {
                    yield break;
                }
            }
            yield break;
        }

        var batch = new List<QueryWorkItem>(maxOpenQueries) { first, source.Current };
        if (maxEvents <= 0) {
            while (true) {
                while (batch.Count < maxOpenQueries && source.MoveNext()) {
                    batch.Add(source.Current);
                }

                foreach (EventObject result in MergeQueryBatch(
                             batch,
                             createEnumerator,
                             maxEvents: 0,
                             oldest: oldest,
                             cancellationToken: cancellationToken)) {
                    yield return result;
                }

                if (!source.MoveNext()) {
                    yield break;
                }

                batch.Clear();
                batch.Add(source.Current);
            }
        }

        var candidates = new List<EventObject>(Math.Min(maxEvents, 256));
        while (true) {
            while (batch.Count < maxOpenQueries && source.MoveNext()) {
                batch.Add(source.Current);
            }

            foreach (EventObject result in MergeQueryBatch(batch, createEnumerator, maxEvents, oldest, cancellationToken)) {
                candidates.Add(result);
            }

            SortAndTrim(candidates, maxEvents, oldest);

            if (!source.MoveNext()) {
                break;
            }

            batch.Clear();
            batch.Add(source.Current);
        }

        candidates.Sort((left, right) => CompareEvents(left, right, oldest));
        int count = maxEvents > 0 ? Math.Min(maxEvents, candidates.Count) : candidates.Count;
        for (int index = 0; index < count; index++) {
            cancellationToken.ThrowIfCancellationRequested();
            yield return candidates[index];
        }
    }

    private static IEnumerable<EventObject> MergeQueryBatch(
        IReadOnlyList<QueryWorkItem> workItems,
        Func<QueryWorkItem, IEnumerator<EventObject>> createEnumerator,
        int maxEvents,
        bool oldest,
        CancellationToken cancellationToken) {

        var cursors = new List<QueryCursor>(workItems.Count);
        var queue = new SortedSet<QueryCursor>(new QueryCursorComparer(oldest));
        int returned = 0;
        try {
            for (int index = 0; index < workItems.Count; index++) {
                QueryWorkItem workItem = workItems[index];
                IEnumerator<EventObject> enumerator = createEnumerator(workItem);
                var cursor = new QueryCursor(index, enumerator);
                cursors.Add(cursor);
                if (cursor.MoveNext()) {
                    queue.Add(cursor);
                }
            }

            while (queue.Count > 0) {
                cancellationToken.ThrowIfCancellationRequested();
                QueryCursor cursor = queue.Min!;
                queue.Remove(cursor);
                yield return cursor.Current;
                returned++;
                if (maxEvents > 0 && returned >= maxEvents) {
                    yield break;
                }
                if (cursor.MoveNext()) {
                    queue.Add(cursor);
                }
            }
        } finally {
            foreach (QueryCursor cursor in cursors) {
                cursor.Dispose();
            }
        }
    }

    private static void SortAndTrim(List<EventObject> candidates, int maxEvents, bool oldest) {
        candidates.Sort((left, right) => CompareEvents(left, right, oldest));
        if (candidates.Count > maxEvents) {
            candidates.RemoveRange(maxEvents, candidates.Count - maxEvents);
        }
    }

    private static int CompareEvents(EventObject left, EventObject right, bool oldest) {
        int recordComparison = oldest
            ? Nullable.Compare(left.RecordId, right.RecordId)
            : Nullable.Compare(right.RecordId, left.RecordId);
        if (recordComparison != 0) {
            return recordComparison;
        }

        int timeComparison = oldest
            ? left.TimeCreated.CompareTo(right.TimeCreated)
            : right.TimeCreated.CompareTo(left.TimeCreated);
        if (timeComparison != 0) {
            return timeComparison;
        }

        int idComparison = left.Id.CompareTo(right.Id);
        return oldest ? idComparison : -idComparison;
    }

    private sealed class QueryCursor : IDisposable {
        private readonly IEnumerator<EventObject> _enumerator;
        private bool _disposed;

        internal QueryCursor(int index, IEnumerator<EventObject> enumerator) {
            Index = index;
            _enumerator = enumerator;
        }

        internal int Index { get; }
        internal EventObject Current => _enumerator.Current;

        internal bool MoveNext() {
            if (_enumerator.MoveNext()) {
                return true;
            }
            Dispose();
            return false;
        }

        public void Dispose() {
            if (_disposed) {
                return;
            }
            _disposed = true;
            _enumerator.Dispose();
        }
    }

    private sealed class QueryCursorComparer : IComparer<QueryCursor> {
        private readonly bool _oldest;

        internal QueryCursorComparer(bool oldest) {
            _oldest = oldest;
        }

        public int Compare(QueryCursor? left, QueryCursor? right) {
            if (ReferenceEquals(left, right)) {
                return 0;
            }
            if (left == null) {
                return 1;
            }
            if (right == null) {
                return -1;
            }

            int recordComparison = CompareEvents(left.Current, right.Current, _oldest);
            if (recordComparison != 0) {
                return recordComparison;
            }
            return left.Index.CompareTo(right.Index);
        }
    }
}

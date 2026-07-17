using System;
using System.Collections.Generic;
using System.Threading;

namespace EventViewerX;

public partial class SearchEvents {
    /// <summary>
    /// Streams events from one or more machines in target order using one global result limit.
    /// </summary>
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
        EventReadMode readMode = EventReadMode.Full) {

        ValidateQueryArguments(logName, maxEvents, sessionTimeoutMs);
        List<string?> targets = machineNames == null || machineNames.Count == 0
            ? new List<string?> { null }
            : machineNames;
        int fixedExpressionCount = CountFixedQueryExpressions(providerName, keywords, level, startTime, endTime, userId, timePeriod);
        int returned = 0;

        foreach (string? machineName in targets) {
            if (maxEvents > 0 && returned >= maxEvents) {
                yield break;
            }

            List<QueryWorkItem> workItems = BuildQueryWorkItems(new List<string?> { machineName }, eventIds, eventRecordId, fixedExpressionCount);
            foreach (EventObject result in MergeQueryWorkItems(
                         workItems,
                         logName,
                         providerName,
                         keywords,
                         level,
                         startTime,
                         endTime,
                         userId,
                         timePeriod,
                         cancellationToken,
                         sessionTimeoutMs ?? Settings.QuerySessionTimeoutMs,
                         readMode)) {
                yield return result;
                returned++;
                if (maxEvents > 0 && returned >= maxEvents) {
                    yield break;
                }
            }
        }
    }

    private static IEnumerable<EventObject> MergeQueryWorkItems(
        List<QueryWorkItem> workItems,
        string logName,
        string? providerName,
        Keywords? keywords,
        Level? level,
        DateTime? startTime,
        DateTime? endTime,
        string? userId,
        TimePeriod? timePeriod,
        CancellationToken cancellationToken,
        int sessionTimeoutMs,
        EventReadMode readMode) {

        var cursors = new List<QueryCursor>(workItems.Count);
        var queue = new SortedSet<QueryCursor>(QueryCursorComparer.Instance);
        try {
            for (int index = 0; index < workItems.Count; index++) {
                QueryWorkItem workItem = workItems[index];
                IEnumerator<EventObject> enumerator = QueryLogEnumerable(
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
                    sessionTimeoutMs: sessionTimeoutMs,
                    readMode: readMode).GetEnumerator();
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
        internal static QueryCursorComparer Instance { get; } = new();

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

            int recordComparison = Nullable.Compare(right.Current.RecordId, left.Current.RecordId);
            if (recordComparison != 0) {
                return recordComparison;
            }
            int timeComparison = right.Current.TimeCreated.CompareTo(left.Current.TimeCreated);
            return timeComparison != 0 ? timeComparison : left.Index.CompareTo(right.Index);
        }
    }
}

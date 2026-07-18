using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    private const int CheckpointCandidateBufferBudget = 4096;
    private const int MaximumCheckpointCandidatePageSize = 1024;

    private static IEnumerable<EventObject> QueryNamedCheckpointCandidates(
        Dictionary<string, HashSet<int>> eventInfo,
        List<string?>? machineNames,
        DateTime? startTime,
        DateTime? endTime,
        TimePeriod? timePeriod,
        CancellationToken cancellationToken,
        Func<string?, string, long?> minimumEventRecordIdExclusiveResolver) {

        List<string?> targets = NormalizeNamedCheckpointTargets(machineNames);
        var pageReaders = new List<Func<int, IReadOnlyList<EventObject>>>(eventInfo.Count * targets.Count);
        foreach (KeyValuePair<string, HashSet<int>> entry in eventInfo) {
            List<int> eventIds = entry.Value.ToList();
            foreach (string? machineName in targets) {
                string logName = entry.Key;
                string? target = machineName;
                long? lowerBound = minimumEventRecordIdExclusiveResolver(target, logName);
                bool completed = false;

                pageReaders.Add(requested => {
                    if (completed) {
                        return Array.Empty<EventObject>();
                    }

                    try {
                        List<EventObject> page = QueryLog(
                            logName,
                            eventIds,
                            target,
                            startTime: startTime,
                            endTime: endTime,
                            maxEvents: requested,
                            timePeriod: timePeriod,
                            cancellationToken: cancellationToken,
                            readMode: EventReadMode.Full,
                            minimumEventRecordIdExclusive: lowerBound,
                            oldest: true).ToList();
                        long? nextLowerBound = null;
                        foreach (EventObject eventObject in page) {
                            if (eventObject.RecordId.HasValue &&
                                (!nextLowerBound.HasValue || eventObject.RecordId.Value > nextLowerBound.Value)) {
                                nextLowerBound = eventObject.RecordId.Value;
                            }
                        }
                        if (!nextLowerBound.HasValue ||
                            (lowerBound.HasValue && nextLowerBound.Value <= lowerBound.Value)) {
                            completed = true;
                        } else {
                            lowerBound = nextLowerBound;
                        }
                        return page;
                    } catch (Exception ex) when (EventLogRemoteQueryFailureClassifier.TryClassify(target, ex, out _)) {
                        completed = true;
                        _logger.WriteWarning(
                            $"Skipping event-log target '{target}' for named-event log '{logName}' after " +
                            $"{ex.GetType().Name}: {ex.Message}");
                        return Array.Empty<EventObject>();
                    }
                });
            }
        }

        if (pageReaders.Count == 0) {
            return Array.Empty<EventObject>();
        }

        return MergePagedSources(
            pageReaders,
            static (left, right) => CompareEvents(left, right, oldest: true),
            GetCheckpointCandidatePageSize(pageReaders.Count),
            cancellationToken);
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

    internal static Func<int, IReadOnlyList<EventObject>> CreateCheckpointPageReader(
        QueryWorkItem initialWorkItem,
        Func<QueryWorkItem, IEnumerator<EventObject>> createEnumerator) {

        if (initialWorkItem == null) {
            throw new ArgumentNullException(nameof(initialWorkItem));
        }
        if (createEnumerator == null) {
            throw new ArgumentNullException(nameof(createEnumerator));
        }

        long? lowerBound = initialWorkItem.MinimumEventRecordIdExclusive;
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
                lowerBound);
            var page = new List<EventObject>(requested);
            using (IEnumerator<EventObject> enumerator = createEnumerator(pageWorkItem)) {
                while (page.Count < requested && enumerator.MoveNext()) {
                    page.Add(enumerator.Current);
                }
            }

            long? nextLowerBound = null;
            foreach (EventObject eventObject in page) {
                if (eventObject.RecordId.HasValue &&
                    (!nextLowerBound.HasValue || eventObject.RecordId.Value > nextLowerBound.Value)) {
                    nextLowerBound = eventObject.RecordId.Value;
                }
            }
            if (page.Count < requested ||
                !nextLowerBound.HasValue ||
                (lowerBound.HasValue && nextLowerBound.Value <= lowerBound.Value)) {
                completed = true;
            } else {
                lowerBound = nextLowerBound;
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

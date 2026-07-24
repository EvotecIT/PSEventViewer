namespace EventViewerX;

/// <summary>
/// Streams a deterministic bounded merge of several native Windows Event Log queries.
/// Each source retains native record order; the next available source head is chosen by timestamp.
/// Windows records can contain non-monotonic timestamps, so this intentionally does not claim a full chronological sort.
/// </summary>
public static partial class EventLogBatchEngine {
    /// <summary>
    /// Reads a channel or file batch while keeping only one detached event per source in merge memory.
    /// </summary>
    public static IEnumerable<EventObject> Read(
        EventLogBatchQuery query,
        CancellationToken cancellationToken = default) {

        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }
        if (query.MaxEvents < 0) {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Maximum events must be greater than or equal to zero.");
        }
        ValidateConcurrency(query.MaxConcurrency);

        EventSourceSnapshot[] sources = SnapshotSources(query);
        bool oldest = sources[0].Oldest;
        if (sources.Any(source => source.Oldest != oldest)) {
            throw new ArgumentException(
                "Every source in a batch must use the same ordering direction.",
                nameof(query));
        }

        return ReadSynchronously(
            query,
            sources,
            oldest,
            cancellationToken);
    }

    private static void ValidateConcurrency(int maxConcurrency) {
        if (maxConcurrency <= 0 ||
            maxConcurrency > EventLogLimits.MaximumConcurrency) {
            throw new ArgumentOutOfRangeException(
                nameof(maxConcurrency),
                $"Maximum concurrency must be between 1 and {EventLogLimits.MaximumConcurrency}.");
        }
    }

    private static EventSourceSnapshot[] SnapshotSources(EventLogBatchQuery query) {
        long sourceLimit = query.MaxEvents;
        EventSourceSnapshot[] channels =
            query.ChannelQueries
                .Select(channel => new EventSourceSnapshot(
                    channel.LogName,
                    channel.MachineName,
                    channel.Oldest,
                    () => EventLogEngine.ReadChannel(
                        CopyChannelQuery(channel, sourceLimit))))
                .ToArray();
        EventSourceSnapshot[] files =
            query.FileQueries
                .Select(file => new EventSourceSnapshot(
                    file.Path,
                    null,
                    file.Oldest,
                    () => EventLogEngine.ReadFile(
                        CopyFileQuery(file, sourceLimit))))
                .ToArray();
        EventSourceSnapshot[] structured =
            query.StructuredQueries
                .Select((structured, index) => new EventSourceSnapshot(
                    $"StructuredQuery[{index}]",
                    structured.MachineName,
                    structured.Oldest,
                    () => EventLogEngine.ReadStructured(
                        CopyStructuredQuery(structured, sourceLimit))))
                .ToArray();
        EventSourceSnapshot[] all = channels
            .Concat(files)
            .Concat(structured)
            .ToArray();
        if (all.Length > 0) {
            return all;
        }
        throw new ArgumentException(
            "The batch does not contain any query sources.",
            nameof(query));
    }

    private static EventLogChannelQuery CopyChannelQuery(
        EventLogChannelQuery source,
        long batchLimit) {

        return new EventLogChannelQuery(source.LogName) {
            MachineName = source.MachineName,
            Credential = source.Credential,
            Authentication = source.Authentication,
            XPath = source.XPath,
            Oldest = source.Oldest,
            ReadMode = source.ReadMode,
            MessageCulture = source.MessageCulture,
            FallbackMessageCulture = source.FallbackMessageCulture,
            MaxEvents = ApplySourceLimit(source.MaxEvents, batchLimit),
            IncludeBookmark = source.IncludeBookmark,
            RemoteConnectionTimeoutMilliseconds =
                source.RemoteConnectionTimeoutMilliseconds,
            RemoteReadTimeoutMilliseconds =
                source.RemoteReadTimeoutMilliseconds,
            BufferCapacity = source.BufferCapacity,
            RpcEndpointPort = source.RpcEndpointPort,
            BookmarkXml = source.BookmarkXml,
            BookmarkOffset = source.BookmarkOffset,
            StrictBookmark = source.StrictBookmark
        };
    }

    private static EventLogFileQuery CopyFileQuery(
        EventLogFileQuery source,
        long batchLimit) {

        return new EventLogFileQuery(source.Path) {
            XPath = source.XPath,
            Oldest = source.Oldest,
            ReadMode = source.ReadMode,
            MessageCulture = source.MessageCulture,
            FallbackMessageCulture = source.FallbackMessageCulture,
            MaxEvents = ApplySourceLimit(source.MaxEvents, batchLimit),
            IncludeBookmark = source.IncludeBookmark,
            BookmarkXml = source.BookmarkXml,
            BookmarkOffset = source.BookmarkOffset,
            StrictBookmark = source.StrictBookmark
        };
    }

    private static EventLogStructuredQuery CopyStructuredQuery(
        EventLogStructuredQuery source,
        long batchLimit) {

        return new EventLogStructuredQuery(source.QueryXml) {
            SourceKind = source.SourceKind,
            MachineName = source.MachineName,
            Credential = source.Credential,
            Authentication = source.Authentication,
            Oldest = source.Oldest,
            ReadMode = source.ReadMode,
            MessageCulture = source.MessageCulture,
            FallbackMessageCulture = source.FallbackMessageCulture,
            MaxEvents = ApplySourceLimit(source.MaxEvents, batchLimit),
            IncludeBookmark = source.IncludeBookmark,
            RemoteConnectionTimeoutMilliseconds =
                source.RemoteConnectionTimeoutMilliseconds,
            RemoteReadTimeoutMilliseconds =
                source.RemoteReadTimeoutMilliseconds,
            BufferCapacity = source.BufferCapacity,
            RpcEndpointPort = source.RpcEndpointPort,
            BookmarkXml = source.BookmarkXml,
            BookmarkOffset = source.BookmarkOffset,
            StrictBookmark = source.StrictBookmark,
            TolerateQueryErrors = source.TolerateQueryErrors,
            FailureHandler = source.FailureHandler
        };
    }

    private static long ApplySourceLimit(
        long sourceLimit,
        long batchLimit) {
        if (batchLimit <= 0) {
            return sourceLimit;
        }
        if (sourceLimit <= 0) {
            return batchLimit;
        }
        return Math.Min(sourceLimit, batchLimit);
    }

    private static IEnumerable<EventObject> ReadSynchronously(
        EventLogBatchQuery query,
        EventSourceSnapshot[] sources,
        bool oldest,
        CancellationToken cancellationToken) {

        EventSourceCursor?[] primed =
            PrimeSourcesSynchronously(
                sources,
                query.MaxConcurrency,
                query.ContinueOnError,
                query.FailureHandler,
                cancellationToken);
        var cursors = primed
            .Where(static cursor => cursor != null)
            .Cast<EventSourceCursor>()
            .ToList();
        var queue = new SortedSet<EventSourceCursor>(
            new EventSourceCursorComparer(oldest));
        foreach (EventSourceCursor cursor in cursors) {
            queue.Add(cursor);
        }

        long returned = 0;
        try {
            while (queue.Count > 0) {
                cancellationToken.ThrowIfCancellationRequested();
                EventSourceCursor cursor = queue.Min!;
                queue.Remove(cursor);
                yield return cursor.Current;
                returned++;
                if (query.MaxEvents > 0 &&
                    returned >= query.MaxEvents) {
                    yield break;
                }

                if (TryMoveNext(
                        cursor,
                        query.ContinueOnError,
                        query.FailureHandler,
                        cancellationToken)) {
                    queue.Add(cursor);
                }
            }
        } finally {
            foreach (EventSourceCursor cursor in cursors) {
                cursor.Dispose();
            }
        }
    }

    private static EventSourceCursor?[] PrimeSourcesSynchronously(
        EventSourceSnapshot[] sources,
        int maxConcurrency,
        bool continueOnError,
        Action<EventLogQueryFailure>? failureHandler,
        CancellationToken cancellationToken) {

        var primed = new EventSourceCursor?[sources.Length];
        int nextSource = -1;
        int workerCount = Math.Min(
            maxConcurrency,
            sources.Length);
        Task[] workers =
            Enumerable
                .Range(0, workerCount)
                .Select(_ => Task.Run(() => {
                    while (true) {
                        cancellationToken.ThrowIfCancellationRequested();
                        int index = Interlocked.Increment(
                            ref nextSource);
                        if (index >= sources.Length) {
                            break;
                        }

                        EventSourceCursor? cursor = TryOpenCursor(
                            index,
                            sources[index],
                            continueOnError,
                            failureHandler);
                        if (cursor == null) {
                            continue;
                        }
                        try {
                            if (TryMoveNext(
                                    cursor,
                                    continueOnError,
                                    failureHandler,
                                    cancellationToken)) {
                                primed[index] = cursor;
                                cursor = null;
                            }
                        } finally {
                            cursor?.Dispose();
                        }
                    }
                }, CancellationToken.None))
                .ToArray();

        try {
            Task.WhenAll(workers)
                .GetAwaiter()
                .GetResult();
            return primed;
        } catch {
            foreach (EventSourceCursor? cursor in primed) {
                cursor?.Dispose();
            }
            throw;
        }
    }

    private static EventSourceCursor? TryOpenCursor(
        int index,
        EventSourceSnapshot source,
        bool continueOnError,
        Action<EventLogQueryFailure>? failureHandler) {

        try {
            return new EventSourceCursor(
                index,
                source,
                source.Open().GetEnumerator());
        } catch (Exception exception) {
            if (!continueOnError) {
                throw;
            }
            failureHandler?.Invoke(new EventLogQueryFailure(
                source.Source,
                source.MachineName,
                exception));
            return null;
        }
    }

    private static bool TryMoveNext(
        EventSourceCursor cursor,
        bool continueOnError,
        Action<EventLogQueryFailure>? failureHandler,
        CancellationToken cancellationToken) {

        try {
            cancellationToken.ThrowIfCancellationRequested();
            return cursor.MoveNext();
        } catch (OperationCanceledException) {
            throw;
        } catch (Exception exception) {
            if (!continueOnError) {
                throw;
            }
            failureHandler?.Invoke(new EventLogQueryFailure(
                cursor.Source.Source,
                cursor.Source.MachineName,
                exception));
            cursor.Dispose();
            return false;
        }
    }

    private static int CompareEvents(
        EventObject left,
        EventObject right,
        bool oldest) {

        int timeComparison = oldest
            ? left.TimeCreated.CompareTo(right.TimeCreated)
            : right.TimeCreated.CompareTo(left.TimeCreated);
        if (timeComparison != 0) {
            return timeComparison;
        }

        int machineComparison = string.Compare(
            left.GatheredFrom,
            right.GatheredFrom,
            StringComparison.OrdinalIgnoreCase);
        if (machineComparison != 0) {
            return oldest ? machineComparison : -machineComparison;
        }

        int logComparison = string.Compare(
            left.LogName,
            right.LogName,
            StringComparison.OrdinalIgnoreCase);
        if (logComparison != 0) {
            return oldest ? logComparison : -logComparison;
        }

        int recordComparison = oldest
            ? Nullable.Compare(left.RecordId, right.RecordId)
            : Nullable.Compare(right.RecordId, left.RecordId);
        if (recordComparison != 0) {
            return recordComparison;
        }

        int idComparison = left.Id.CompareTo(right.Id);
        return oldest ? idComparison : -idComparison;
    }

    private sealed class EventSourceSnapshot {
        internal EventSourceSnapshot(
            string source,
            string? machineName,
            bool oldest,
            Func<IEnumerable<EventObject>> open) {

            Source = source;
            MachineName = machineName;
            Oldest = oldest;
            Open = open;
        }

        internal string Source { get; }
        internal string? MachineName { get; }
        internal bool Oldest { get; }
        internal Func<IEnumerable<EventObject>> Open { get; }
    }

    private sealed class EventSourceCursor : IDisposable {
        private readonly IEnumerator<EventObject> _enumerator;
        private bool _disposed;

        internal EventSourceCursor(
            int index,
            EventSourceSnapshot source,
            IEnumerator<EventObject> enumerator) {

            Index = index;
            Source = source;
            _enumerator = enumerator;
        }

        internal int Index { get; }
        internal EventSourceSnapshot Source { get; }
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

    private sealed class EventSourceCursorComparer : IComparer<EventSourceCursor> {
        private readonly bool _oldest;

        internal EventSourceCursorComparer(bool oldest) {
            _oldest = oldest;
        }

        public int Compare(
            EventSourceCursor? left,
            EventSourceCursor? right) {

            if (ReferenceEquals(left, right)) {
                return 0;
            }
            if (left == null) {
                return 1;
            }
            if (right == null) {
                return -1;
            }

            int eventComparison = CompareEvents(
                left.Current,
                right.Current,
                _oldest);
            return eventComparison != 0
                ? eventComparison
                : left.Index.CompareTo(right.Index);
        }
    }
}

using System.Runtime.CompilerServices;

namespace EventViewerX;

public static partial class EventLogBatchEngine {
    /// <summary>
    /// Asynchronously reads a bounded deterministic merge. Independent sources are primed concurrently,
    /// while consumer backpressure keeps only one detached head record per source in merge memory.
    /// </summary>
    public static IAsyncEnumerable<EventObject> ReadAsync(
        EventLogBatchQuery query,
        CancellationToken cancellationToken = default) {

        return ReadAsynchronously(
            CreateExecutionPlan(query),
            cancellationToken);
    }

    private static async IAsyncEnumerable<EventObject>
        ReadAsynchronously(
            EventLogBatchExecutionPlan plan,
            [EnumeratorCancellation]
            CancellationToken cancellationToken) {

        using var concurrencyGate =
            new SemaphoreSlim(
                plan.MaxConcurrency,
                plan.MaxConcurrency);
        Task<EventSourceCursor?>[] primeTasks =
            plan.Sources
                .Select((source, index) =>
                    PrimeSourceAsync(
                        index,
                        source,
                        plan.ContinueOnError,
                        plan.FailureHandler,
                        concurrencyGate,
                        cancellationToken))
                .ToArray();
        EventSourceCursor?[] primed;
        try {
            primed = await Task.WhenAll(primeTasks)
                .ConfigureAwait(false);
        } catch {
            foreach (Task<EventSourceCursor?> task in primeTasks) {
                if (task.Status == TaskStatus.RanToCompletion) {
                    task.Result?.Dispose();
                }
            }
            throw;
        }
        var cursors = primed
            .Where(static cursor => cursor != null)
            .Cast<EventSourceCursor>()
            .ToList();
        var queue = new SortedSet<EventSourceCursor>(
            new EventSourceCursorComparer(plan.Oldest));
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
                if (plan.MaxEvents > 0 &&
                    returned >= plan.MaxEvents) {
                    yield break;
                }

                bool hasNext = await Task.Run(
                        () => TryMoveNext(
                            cursor,
                            plan.ContinueOnError,
                            plan.FailureHandler,
                            cancellationToken),
                        CancellationToken.None)
                    .ConfigureAwait(false);
                if (hasNext) {
                    queue.Add(cursor);
                }
            }
        } finally {
            foreach (EventSourceCursor cursor in cursors) {
                cursor.Dispose();
            }
        }
    }

    private static async Task<EventSourceCursor?> PrimeSourceAsync(
        int index,
        EventSourceSnapshot source,
        bool continueOnError,
        Action<EventLogQueryFailure>? failureHandler,
        SemaphoreSlim concurrencyGate,
        CancellationToken cancellationToken) {

        await concurrencyGate.WaitAsync(
                cancellationToken)
            .ConfigureAwait(false);
        try {
            return await Task.Run(() => {
                EventSourceCursor? cursor = TryOpenCursor(
                    index,
                    source,
                    continueOnError,
                    failureHandler,
                    cancellationToken);
                if (cursor == null) {
                    return null;
                }
                try {
                    if (TryMoveNext(
                            cursor,
                            continueOnError,
                            failureHandler,
                            cancellationToken)) {
                        EventSourceCursor result = cursor;
                        cursor = null;
                        return result;
                    }
                    return null;
                } finally {
                    cursor?.Dispose();
                }
            }, CancellationToken.None).ConfigureAwait(false);
        } finally {
            concurrencyGate.Release();
        }
    }
}

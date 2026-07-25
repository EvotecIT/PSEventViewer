using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace EventViewerX;

public static partial class EventLogEngine {
    /// <summary>Asynchronously streams a local or remote channel through bounded memory.</summary>
    public static IAsyncEnumerable<EventObject> ReadChannelAsync(
        EventLogChannelQuery query,
        CancellationToken cancellationToken = default) {

        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }
        return ReadAsync(
            token => ReadChannel(query, token),
            query.BufferCapacity,
            cancellationToken);
    }

    /// <summary>Asynchronously streams an offline event log through bounded memory.</summary>
    public static IAsyncEnumerable<EventObject> ReadFileAsync(
        EventLogFileQuery query,
        int bufferCapacity = 64,
        CancellationToken cancellationToken = default) {

        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }
        return ReadAsync(
            token => ReadFile(query, token),
            bufferCapacity,
            cancellationToken);
    }

    /// <summary>Asynchronously streams a structured QueryList through bounded memory.</summary>
    public static IAsyncEnumerable<EventObject> ReadStructuredAsync(
        EventLogStructuredQuery query,
        CancellationToken cancellationToken = default) {

        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }
        return ReadAsync(
            token => ReadStructured(query, token),
            query.BufferCapacity,
            cancellationToken);
    }

    /// <summary>Asynchronously streams a deterministic multi-source batch through bounded memory.</summary>
    public static IAsyncEnumerable<EventObject> ReadBatchAsync(
        EventLogBatchQuery query,
        CancellationToken cancellationToken = default) {

        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }
        return EventLogBatchEngine.ReadAsync(
            query,
            cancellationToken);
    }

    private static async IAsyncEnumerable<EventObject> ReadAsync(
        Func<CancellationToken, IEnumerable<EventObject>> source,
        int bufferCapacity,
        [EnumeratorCancellation] CancellationToken cancellationToken) {

        if (bufferCapacity <= 0 || bufferCapacity > 4096) {
            throw new ArgumentOutOfRangeException(
                nameof(bufferCapacity),
                "Buffer capacity must be between 1 and 4096.");
        }
        using var stop = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);
        Channel<EventObject> channel =
            Channel.CreateBounded<EventObject>(
                new BoundedChannelOptions(bufferCapacity) {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = false
                });
        Task producer = Task.Run(async () => {
            try {
                foreach (EventObject eventObject in source(stop.Token)) {
                    await channel.Writer.WriteAsync(
                        eventObject,
                        stop.Token).ConfigureAwait(false);
                }
                channel.Writer.TryComplete();
            } catch (OperationCanceledException)
                when (stop.IsCancellationRequested) {
                channel.Writer.TryComplete();
            } catch (Exception exception) {
                channel.Writer.TryComplete(exception);
            }
        }, CancellationToken.None);

        try {
            while (await channel.Reader.WaitToReadAsync(
                       cancellationToken).ConfigureAwait(false)) {
                while (channel.Reader.TryRead(
                           out EventObject? eventObject)) {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return eventObject;
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
        } finally {
            stop.Cancel();
            await producer.ConfigureAwait(false);
        }
    }
}

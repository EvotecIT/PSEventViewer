using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using EventViewerX.Native;

namespace EventViewerX;

internal static class EventLogNativeOperation {
    internal static T Execute<T>(
        Func<T> operation,
        int timeoutMilliseconds,
        string timeoutMessage,
        Action<T>? lateResultCleanup = null) {

        return BoundedNativeOperation.Execute(
            operation,
            timeoutMilliseconds,
            timeoutMessage,
            lateResultCleanup);
    }

    internal static EventLogReader CreateReader(
        EventLogQuery query,
        string? machineName,
        int timeoutMilliseconds = 0) {

        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }
        if (timeoutMilliseconds <= 0) {
            return new EventLogReader(query);
        }

        string target = string.IsNullOrWhiteSpace(machineName)
            ? "the local computer"
            : $"'{machineName}'";
        return Execute(
            () => new EventLogReader(query),
            timeoutMilliseconds,
            $"Timed out creating an Event Log reader for {target} after {timeoutMilliseconds} ms.",
            static reader => reader.Dispose());
    }

    internal static EventRecord? ReadEvent(
        EventLogReader reader,
        int timeoutMilliseconds,
        string operation) {

        if (timeoutMilliseconds <= 0) {
            return reader.ReadEvent();
        }

        TimeSpan timeout =
            TimeSpan.FromMilliseconds(timeoutMilliseconds);
        var stopwatch = Stopwatch.StartNew();
        EventRecord? record = reader.ReadEvent(timeout);
        if (record == null &&
            stopwatch.Elapsed.Ticks >=
            timeout.Ticks * 9 / 10) {
            throw new TimeoutException(
                $"{operation} exceeded its {timeoutMilliseconds} ms native read window.");
        }
        return record;
    }
}

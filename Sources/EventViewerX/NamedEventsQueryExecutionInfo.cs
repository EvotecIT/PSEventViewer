using System.Collections.Concurrent;

namespace EventViewerX;

/// <summary>
/// Reports candidate-scan progress for a named-event query.
/// </summary>
public sealed class NamedEventsQueryExecutionInfo {
    private readonly ConcurrentDictionary<string, EventLogQueryTargetFailure> _targetFailures =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Number of candidate event records evaluated by named-event rules.
    /// </summary>
    public long EventsScanned { get; internal set; }

    /// <summary>
    /// Number of named-event matches emitted to the caller.
    /// </summary>
    public long EventsEmitted { get; internal set; }

    /// <summary>
    /// Effective candidate scan cap. Zero means unlimited.
    /// </summary>
    public int MaxEventsScanned { get; internal set; }

    /// <summary>
    /// Indicates that another candidate existed beyond <see cref="MaxEventsScanned"/>.
    /// </summary>
    public bool ScanLimitReached { get; internal set; }

    /// <summary>
    /// Remote targets that could not be queried. Healthy targets may still have emitted results.
    /// </summary>
    public IReadOnlyList<EventLogQueryTargetFailure> TargetFailures => _targetFailures.Values
        .OrderBy(static failure => failure.MachineName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    internal void Reset(int maxEventsScanned) {
        EventsScanned = 0;
        EventsEmitted = 0;
        MaxEventsScanned = maxEventsScanned;
        ScanLimitReached = false;
        _targetFailures.Clear();
    }

    internal bool TryRecordCandidate() {
        if (MaxEventsScanned > 0 && EventsScanned >= MaxEventsScanned) {
            ScanLimitReached = true;
            return false;
        }

        EventsScanned++;
        return true;
    }

    internal void RecordTargetFailure(EventLogQueryTargetFailure failure) {
        if (failure == null ||
            string.IsNullOrWhiteSpace(failure.MachineName) ||
            failure.Kind == EventLogRemoteQueryFailureKind.None) {
            return;
        }

        _targetFailures.TryAdd(failure.MachineName, failure);
    }
}

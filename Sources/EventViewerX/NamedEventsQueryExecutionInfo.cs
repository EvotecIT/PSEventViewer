namespace EventViewerX;

/// <summary>
/// Reports candidate-scan progress for a named-event query.
/// </summary>
public sealed class NamedEventsQueryExecutionInfo {
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

    internal void Reset(int maxEventsScanned) {
        EventsScanned = 0;
        EventsEmitted = 0;
        MaxEventsScanned = maxEventsScanned;
        ScanLimitReached = false;
    }

    internal bool TryRecordCandidate() {
        if (MaxEventsScanned > 0 && EventsScanned >= MaxEventsScanned) {
            ScanLimitReached = true;
            return false;
        }

        EventsScanned++;
        return true;
    }
}

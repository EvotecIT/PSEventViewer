namespace EventViewerX;

/// <summary>Describes how a bounded PowerShell script-log query completed.</summary>
public sealed class PowerShellScriptQueryExecutionInfo {
    /// <summary>Computer queried, or an empty string for the local computer.</summary>
    public string MachineName { get; internal set; } = string.Empty;

    /// <summary>Exported EVTX path queried, or an empty string for a live log.</summary>
    public string EventLogPath { get; internal set; } = string.Empty;

    /// <summary>Maximum output items requested. Zero means no explicit output limit.</summary>
    public int MaxResults { get; internal set; }

    /// <summary>Maximum native records requested. Zero means no explicit scan limit.</summary>
    public int MaxEventsScanned { get; internal set; }

    /// <summary>Maximum incomplete script groups retained, or zero for execution-record queries.</summary>
    public int MaxPendingScripts { get; internal set; }

    /// <summary>Maximum incomplete-group event snapshots retained, or zero for execution-record queries.</summary>
    public int MaxCachedEvents { get; internal set; }

    /// <summary>Number of native records inspected.</summary>
    public int EventsScanned { get; internal set; }

    /// <summary>Number of execution records or reconstructed scripts emitted.</summary>
    public int ResultsReturned { get; internal set; }

    /// <summary>
    /// Indicates that enumeration stopped at the configured scan limit and additional records may exist.
    /// </summary>
    public bool ScanLimitReached { get; internal set; }

    /// <summary>
    /// Indicates that enumeration stopped at the configured output limit and additional results may exist.
    /// </summary>
    public bool OutputLimitReached { get; internal set; }

    /// <summary>Number of incomplete script groups evicted after reaching cache bounds.</summary>
    public int EvictedIncompleteScripts { get; internal set; }

    /// <summary>Number of cached event snapshots released with evicted incomplete groups.</summary>
    public int EvictedCachedEvents { get; internal set; }

    /// <summary>Number of event records with invalid or excessive fragment numbering metadata.</summary>
    public int InvalidFragmentMetadataEvents { get; internal set; }

    /// <summary>Number of bounded end-of-query script results missing declared fragments.</summary>
    public int IncompleteScriptsReturned { get; internal set; }

    /// <summary>Indicates that the caller should not treat the query as a complete view of all matching scripts.</summary>
    public bool MayBeIncomplete =>
        ScanLimitReached ||
        OutputLimitReached ||
        EvictedIncompleteScripts > 0 ||
        InvalidFragmentMetadataEvents > 0 ||
        IncompleteScriptsReturned > 0;

    internal void Reset(
        string? machineName,
        string? eventLogPath,
        int maxResults,
        int maxEventsScanned,
        int maxPendingScripts = 0,
        int maxCachedEvents = 0) {
        MachineName = machineName ?? string.Empty;
        EventLogPath = eventLogPath ?? string.Empty;
        MaxResults = maxResults;
        MaxEventsScanned = maxEventsScanned;
        MaxPendingScripts = maxPendingScripts;
        MaxCachedEvents = maxCachedEvents;
        EventsScanned = 0;
        ResultsReturned = 0;
        ScanLimitReached = false;
        OutputLimitReached = false;
        EvictedIncompleteScripts = 0;
        EvictedCachedEvents = 0;
        InvalidFragmentMetadataEvents = 0;
        IncompleteScriptsReturned = 0;
    }
}

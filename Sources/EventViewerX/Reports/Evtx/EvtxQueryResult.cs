using System.Collections.Generic;

namespace EventViewerX.Reports.Evtx;

/// <summary>
/// Query result for EVTX reads.
/// </summary>
public sealed class EvtxQueryResult {
    /// <summary>
    /// Gets or sets queried events.
    /// </summary>
    public IReadOnlyList<EventObject> Events { get; set; } = new List<EventObject>();
}

using System.Globalization;
using System.Net;

namespace EventViewerX;

/// <summary>
/// Defines a reusable named-event query over the native Windows Event Log engine.
/// </summary>
public sealed class NamedEventQuery {
    /// <summary>Creates a query for one or more registered named-event rules.</summary>
    public NamedEventQuery(
        IEnumerable<NamedEvents> namedEvents) {

        if (namedEvents == null) {
            throw new ArgumentNullException(
                nameof(namedEvents));
        }
        NamedEvents = namedEvents
            .Distinct()
            .ToArray();
        if (NamedEvents.Count == 0) {
            throw new ArgumentException(
                "At least one named event is required.",
                nameof(namedEvents));
        }
        foreach (NamedEvents namedEvent in NamedEvents) {
            if (!Enum.IsDefined(
                    typeof(NamedEvents),
                    namedEvent)) {
                throw new ArgumentOutOfRangeException(
                    nameof(namedEvents),
                    namedEvent,
                    $"Named event value '{namedEvent}' is not registered.");
            }
        }
    }

    /// <summary>Named-event rules selected by this query.</summary>
    public IReadOnlyList<NamedEvents> NamedEvents {
        get;
    }

    /// <summary>Local or remote machines. Null or empty targets the local machine.</summary>
    public IReadOnlyList<string?>? MachineNames {
        get;
        set;
    }

    /// <summary>Optional exact source channel.</summary>
    public string? SourceLogName { get; set; }

    /// <summary>Optional source event identifiers.</summary>
    public IReadOnlyCollection<int>? SourceEventIds {
        get;
        set;
    }

    /// <summary>Earliest event time.</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>Latest event time.</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>Relative time window.</summary>
    public TimePeriod? TimePeriod { get; set; }

    /// <summary>Maximum matching named events. Zero streams every match.</summary>
    public long MaxEvents { get; set; }

    /// <summary>Maximum raw candidates evaluated by rules. Zero is unlimited.</summary>
    public long MaxCandidates { get; set; }

    /// <summary>Maximum independent sources opened concurrently.</summary>
    public int MaxConcurrency { get; set; } = 8;

    /// <summary>Whether records are read oldest first.</summary>
    public bool Oldest { get; set; }

    /// <summary>
    /// Amount of source event data materialized before named-event projection.
    /// Full preserves every rule; StructuredData is faster when selected rules use only XML payload fields.
    /// </summary>
    public EventReadMode ReadMode { get; set; } =
        EventReadMode.Full;

    /// <summary>Materializes a native bookmark for every projected source event.</summary>
    public bool IncludeBookmark { get; set; }

    /// <summary>Remote credentials shared by every remote target.</summary>
    public NetworkCredential? Credential { get; set; }

    /// <summary>Remote authentication package.</summary>
    public EventLogAuthentication Authentication {
        get;
        set;
    }

    /// <summary>Maximum time used to establish each remote session.</summary>
    public int RemoteConnectionTimeoutMilliseconds {
        get;
        set;
    } = 5000;

    /// <summary>Maximum time without remote read progress. Zero is unbounded.</summary>
    public int RemoteReadTimeoutMilliseconds {
        get;
        set;
    }

    /// <summary>Detached snapshots buffered by each remote reader.</summary>
    public int BufferCapacity { get; set; } = 64;

    /// <summary>Culture used for provider messages and display names.</summary>
    public CultureInfo? MessageCulture { get; set; }

    /// <summary>Fallback provider-resource culture.</summary>
    public CultureInfo? FallbackMessageCulture {
        get;
        set;
    }

    /// <summary>Optional ordered post-projection enrichment.</summary>
    public NamedEventEnrichmentOptions? Enrichment {
        get;
        set;
    }

    /// <summary>Optional predicate applied after named-event projection.</summary>
    public Func<EventObjectSlim, bool>? ResultPredicate {
        get;
        set;
    }

    /// <summary>
    /// Optional per-machine and per-channel exclusive record checkpoint.
    /// </summary>
    public Func<string?, string, long?>? MinimumRecordIdExclusiveResolver {
        get;
        set;
    }

    /// <summary>
    /// Optional observer invoked in source order after projection and enrichment complete.
    /// </summary>
    public Action<EventObject>? CandidateObserver {
        get;
        set;
    }

    /// <summary>
    /// Continues healthy remote targets after an expected remote-target failure.
    /// Local and programming failures always terminate.
    /// </summary>
    public bool ContinueOnRemoteFailure { get; set; } = true;
}

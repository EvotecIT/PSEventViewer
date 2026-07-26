namespace EventViewerX;

/// <summary>
/// Typed Windows Event Log filter shared by library, PowerShell, CLI, and service hosts.
/// </summary>
public sealed class EventFilter {
    /// <summary>Event identifiers to include.</summary>
    public IReadOnlyList<int>? EventIds { get; set; }

    /// <summary>Event record identifiers to include.</summary>
    public IReadOnlyList<long>? RecordIds { get; set; }

    /// <summary>Optional exclusive lower event-record boundary.</summary>
    public long? MinimumRecordIdExclusive { get; set; }

    /// <summary>Optional exclusive upper event-record boundary.</summary>
    public long? MaximumRecordIdExclusive { get; set; }

    /// <summary>Provider names to include.</summary>
    public IReadOnlyList<string>? ProviderNames { get; set; }

    /// <summary>Numeric event levels to include.</summary>
    public IReadOnlyList<byte>? Levels { get; set; }

    /// <summary>Keyword masks to include.</summary>
    public IReadOnlyList<long>? Keywords { get; set; }

    /// <summary>Earliest event time to include.</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>Latest event time to include.</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>User SIDs or resolvable account names to include.</summary>
    public IReadOnlyList<string>? UserIds { get; set; }

    /// <summary>Unnamed EventData values to include.</summary>
    public IReadOnlyList<string>? Data { get; set; }

    /// <summary>Named EventData values to include.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? NamedData { get; set; }

    /// <summary>Named EventData values to exclude.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? ExcludedNamedData { get; set; }

    /// <summary>Event identifiers to exclude.</summary>
    public IReadOnlyList<int>? ExcludedEventIds { get; set; }

    /// <summary>Returns true when at least one filter dimension is populated.</summary>
    public bool HasAny =>
        (EventIds?.Count ?? 0) > 0 ||
        (RecordIds?.Count ?? 0) > 0 ||
        MinimumRecordIdExclusive.HasValue ||
        MaximumRecordIdExclusive.HasValue ||
        (ProviderNames?.Count ?? 0) > 0 ||
        (Levels?.Count ?? 0) > 0 ||
        (Keywords?.Count ?? 0) > 0 ||
        StartTime.HasValue ||
        EndTime.HasValue ||
        (UserIds?.Count ?? 0) > 0 ||
        (Data?.Count ?? 0) > 0 ||
        (NamedData?.Count ?? 0) > 0 ||
        (ExcludedNamedData?.Count ?? 0) > 0 ||
        (ExcludedEventIds?.Count ?? 0) > 0;
}

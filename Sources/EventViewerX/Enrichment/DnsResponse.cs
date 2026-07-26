namespace EventViewerX;

/// <summary>Outcome of a reverse-DNS lookup used by optional event enrichment.</summary>
public enum DnsResponseCode {
    /// <summary>The lookup completed.</summary>
    NoError = 0,
    /// <summary>The address has no reverse record.</summary>
    NXDomain = 1,
    /// <summary>The system resolver failed.</summary>
    ServerFailure = 2
}

/// <summary>DNS record types used by event enrichment.</summary>
public enum DnsRecordType {
    /// <summary>Reverse-address pointer record.</summary>
    PTR = 12
}

/// <summary>One detached DNS answer.</summary>
public sealed class DnsAnswer {
    /// <summary>Record type.</summary>
    public DnsRecordType Type { get; set; }

    /// <summary>Raw answer value.</summary>
    public string DataRaw { get; set; } = string.Empty;

    /// <summary>Normalized answer value.</summary>
    public string Data => DataRaw;
}

/// <summary>Dependency-free reverse-DNS response.</summary>
public sealed class DnsResponse {
    /// <summary>Resolver status.</summary>
    public DnsResponseCode Status { get; set; }

    /// <summary>Returned answers.</summary>
    public IReadOnlyList<DnsAnswer> Answers { get; set; } =
        Array.Empty<DnsAnswer>();

    /// <summary>Resolver diagnostic when Status is not NoError.</summary>
    public string Error { get; set; } = string.Empty;
}

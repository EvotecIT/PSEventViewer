namespace EventViewerX;

/// <summary>
/// Describes the impersonation level used during authentication.
/// </summary>
public enum ImpersonationLevel {
    Identification = 1832,
    Impersonation = 1833,
    Delegation = 1840,
    DeniedByProcessTrustLabelACE = 1841,
    Yes = 1842,
    No = 1843,
    System = 1844,
    NotAvailable = 1845,
    Default = 1846,
    DisallowMmConfig = 1847,
    Off = 1848,
    Auto = 1849
}

namespace EventViewerX;

/// <summary>
/// NTSTATUS codes returned for logon failures.
/// </summary>
public enum StatusCode : uint {
    /// <summary>No status reported.</summary>
    None = 0x0,
    // NTSTATUS codes for logon failures
    /// <summary>Unknown username or bad password.</summary>
    StatusLogonFailure = 0xC000006D,
    /// <summary>User account restrictions.</summary>
    StatusAccountRestriction = 0xC000006E,
    /// <summary>Invalid logon hours.</summary>
    StatusInvalidLogonHours = 0xC000006F,
    /// <summary>Invalid workstation.</summary>
    StatusInvalidWorkstation = 0xC0000070,
    /// <summary>Password expired.</summary>
    StatusPasswordExpired = 0xC0000071,
    /// <summary>Account disabled.</summary>
    StatusAccountDisabled = 0xC0000072,
    /// <summary>Account locked out.</summary>
    StatusAccountLockedOut = 0xC0000234,
    /// <summary>Account expired.</summary>
    StatusAccountExpired = 0xC0000193,
    /// <summary>Logon type not granted.</summary>
    StatusLogonTypeNotGranted = 0xC000015B,
    /// <summary>No trust SAM account.</summary>
    StatusNoTrust = 0xC000005E
}

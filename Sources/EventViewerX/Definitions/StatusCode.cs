namespace EventViewerX;

/// <summary>
/// NTSTATUS codes returned for logon failures.
/// </summary>
public enum StatusCode : uint {
    None = 0x0,
    // NTSTATUS codes for logon failures
    StatusLogonFailure = 0xC000006D,           // Unknown username or bad password
    StatusAccountRestriction = 0xC000006E,     // User account restrictions
    StatusInvalidLogonHours = 0xC000006F,      // Invalid logon hours
    StatusInvalidWorkstation = 0xC0000070,     // Invalid workstation
    StatusPasswordExpired = 0xC0000071,        // Password expired
    StatusAccountDisabled = 0xC0000072,        // Account disabled
    StatusAccountLockedOut = 0xC0000234,       // Account locked out
    StatusAccountExpired = 0xC0000193,         // Account expired
    StatusLogonTypeNotGranted = 0xC000015B,    // Logon type not granted
    StatusNoTrust = 0xC000005E                 // No trust SAM account
}

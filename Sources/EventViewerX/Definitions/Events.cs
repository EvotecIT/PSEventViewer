namespace EventViewerX;

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

public enum VirtualAccount {
    Yes = 1843,
    No = 1844
}

public enum ElevatedToken {
    Yes = 1842,
    No = 1843
}

public enum LogonType {
    Interactive = 2,
    Network = 3,
    Batch = 4,
    Service = 5,
    Unlock = 7,
    NetworkCleartext = 8,
    NewCredentials = 9,
    RemoteInteractive = 10,
    CachedInteractive = 11
}

public enum TicketOptions {
    // Add appropriate values here
}

public enum Status {
    // Add appropriate values here
}

public enum TicketEncryptionType {
    DES_CBC_CRC = 0x1,
    DES_CBC_MD5 = 0x3,
    AES128_CTS_HMAC_SHA1_96 = 0x11,
    AES256_CTS_HMAC_SHA1_96 = 0x12,
    RC4_HMAC = 0x17,
    RC4_HMAC_EXP = 0x18,
    AuditFailure = unchecked((int)0xFFFFFFFF)
}

public enum PreAuthType {
    // Add appropriate values here
}

public enum FailureReason {
    None = 0,
    // Common failure reasons (prefixed with %%)
    UnknownUserNameOrBadPassword = 2304,
    AccountRestrictions = 2305,
    AccountLockedOut = 2307,
    AccountExpired = 2306,
    LogonTypeNotGranted = 2309,
    PasswordExpired = 2308
}

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

public enum SubStatusCode : uint {
    None = 0x0,
    // Add any specific substatus codes as they are discovered
    StatusInvalidLogonHours = 0xC000006F,
    StatusInvalidWorkstation = 0xC0000070,
    StatusPasswordExpired = 0xC0000071,
    StatusAccountDisabled = 0xC0000072
}
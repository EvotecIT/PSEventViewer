namespace EventViewerX;

/// <summary>
/// Combined set of NTSTATUS values (logon failures) and Kerberos KDC error/status codes used by security events.
/// </summary>
public enum StatusCode : uint
{
    /// <summary>Request succeeded / no error.</summary>
    None = 0x00000000,

    // Kerberos KDC status codes (RFC 4120 / MS-KILE)
    KdcErrNameExp = 0x00000001,
    KdcErrServiceExp = 0x00000002,
    KdcErrBadPvno = 0x00000003,
    KdcErrCOldMastKvno = 0x00000004,
    KdcErrSOldMastKvno = 0x00000005,
    KdcErrCPrincipalUnknown = 0x00000006,
    KdcErrSPrincipalUnknown = 0x00000007,
    KdcErrPrincipalNotUnique = 0x00000008,
    KdcErrNullKey = 0x00000009,
    KdcErrCannotPostdate = 0x0000000A,
    KdcErrNeverValid = 0x0000000B,
    KdcErrPolicy = 0x0000000C,
    KdcErrBadOption = 0x0000000D,
    KdcErrEtypeNotSupp = 0x0000000E,
    KdcErrSumtypeNotSupp = 0x0000000F,
    KdcErrPadataTypeNotSupp = 0x00000010,
    KdcErrTrtypeNotSupp = 0x00000011,
    KdcErrClientRevoked = 0x00000012,
    KdcErrServiceRevoked = 0x00000013,
    KdcErrTgtRevoked = 0x00000014,
    KdcErrClientNotYet = 0x00000015,
    KdcErrServiceNotYet = 0x00000016,
    KdcErrKeyExpired = 0x00000017,
    KdcErrPreauthFailed = 0x00000018,
    KdcErrPreauthRequired = 0x00000019,
    KdcErrServerNOMatch = 0x0000001A,
    KdcErrSKeyExpired = 0x0000001B,
    KdcErrAuthExp = 0x0000001C,
    KdcErrPreauthBadIntegrity = 0x0000001D,
    KdcErrAdditionalTickets = 0x0000001F,

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

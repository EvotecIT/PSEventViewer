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


// Enums for the new methods
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

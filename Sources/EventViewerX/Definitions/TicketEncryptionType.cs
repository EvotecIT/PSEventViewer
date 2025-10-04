namespace EventViewerX;

/// <summary>
/// Identifies encryption algorithms used for Kerberos tickets.
/// </summary>
public enum TicketEncryptionType {
    DES_CBC_CRC = 0x1,
    DES_CBC_MD5 = 0x3,
    AES128_CTS_HMAC_SHA1_96 = 0x11,
    AES256_CTS_HMAC_SHA1_96 = 0x12,
    RC4_HMAC = 0x17,
    RC4_HMAC_EXP = 0x18,
    AuditFailure = unchecked((int)0xFFFFFFFF)
}

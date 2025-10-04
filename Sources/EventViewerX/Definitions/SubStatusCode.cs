namespace EventViewerX;

/// <summary>
/// Additional sub-status codes for failed authentication.
/// </summary>
public enum SubStatusCode : uint {
    None = 0x0,
    // Add any specific substatus codes as they are discovered
    StatusInvalidLogonHours = 0xC000006F,
    StatusInvalidWorkstation = 0xC0000070,
    StatusPasswordExpired = 0xC0000071,
    StatusAccountDisabled = 0xC0000072
}

namespace EventViewerX;

/// <summary>
/// Represents a single Group Policy link with its display name, GUID, and distinguished name.
/// </summary>
public class GpoLink {
    public string DisplayName { get; set; } = string.Empty;
    public string Guid { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

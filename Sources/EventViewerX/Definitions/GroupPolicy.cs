namespace EventViewerX;

/// <summary>
/// Represents a single Group Policy with its name, ID, and domain.
/// </summary>
public class GroupPolicy {
    /// <summary>Name of the Group Policy Object.</summary>
    public string GpoName { get; set; }
    /// <summary>Identifier of the Group Policy Object.</summary>
    public string GpoId { get; set; }
    /// <summary>Domain hosting the Group Policy Object.</summary>
    public string GpoDomain { get; set; }
}
namespace EventViewerX;

/// <summary>
/// Represents a single Group Policy with its name, ID, and domain.
/// </summary>
public class GroupPolicy {
    public string GpoName { get; set; }
    public string GpoId { get; set; }
    public string GpoDomain { get; set; }
}
namespace EventViewerX;

/// <summary>
/// Represents a reconstructed PowerShell script from event logs.
/// </summary>
public class RestoredPowerShellScript {
    /// <summary>
    /// Identifier of the script block.
    /// </summary>
    public string ScriptBlockId { get; set; } = string.Empty;

    /// <summary>
    /// Full script text reconstructed from events.
    /// </summary>
    public string Script { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether every numbered fragment declared by the event stream was available.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Number of script fragments declared by the event stream.
    /// </summary>
    public int ExpectedPartCount { get; set; }

    /// <summary>
    /// Number of distinct script fragments that were available for reconstruction.
    /// </summary>
    public int AvailablePartCount { get; set; }

    /// <summary>
    /// Managed event snapshots that compose the script.
    /// </summary>
    public IReadOnlyList<EventObject> Events { get; set; } = Array.Empty<EventObject>();

    /// <summary>
    /// Primary event snapshot for convenience access.
    /// </summary>
    public EventObject? Event => Events.Count > 0 ? Events[0] : null;

    /// <summary>
    /// Parsed data dictionary from the event.
    /// </summary>
    public IDictionary<string, string?> Data { get; set; } = new Dictionary<string, string?>();

    /// <summary>
    /// Saves the script to the specified directory.
    /// </summary>
    public string Save(string directory, bool addComment = true, bool unblock = false) {
        EventObject primary = Event ?? throw new InvalidOperationException("No event data available to save script.");
        Directory.CreateDirectory(directory);
        string fileName = $"{primary.MachineName}_{ScriptBlockId}.ps1";
        string filePath = Path.Combine(directory, fileName);
        if (addComment) {
            string header = string.Join(Environment.NewLine,
                "<#",
                $"RecordID = {primary.RecordId}",
                $"LogName = {primary.LogName}",
                $"MachineName = {primary.MachineName}",
                $"TimeCreated = {primary.TimeCreated}",
                "#>");
            File.WriteAllText(filePath, header + Environment.NewLine + Script);
        } else {
            File.WriteAllText(filePath, Script);
        }
        if (!unblock) {
            File.WriteAllText(filePath + ":Zone.Identifier", "[ZoneTransfer]\r\nZoneId=3");
        }
        return filePath;
    }
}

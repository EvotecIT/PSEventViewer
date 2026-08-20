namespace EventViewerX;

/// <summary>Declares one projected property for a custom event definition.</summary>
public sealed class EventDefinitionField {
    /// <summary>Output property name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Optional human-friendly report heading. The field name is used when empty.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Optional field purpose shown by discovery and report surfaces.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Alternative field names accepted by predicate builders.</summary>
    public IReadOnlyList<string> Aliases { get; set; } = Array.Empty<string>();
    /// <summary>Portable output value type.</summary>
    public EventFieldValueKind ValueKind { get; set; } = EventFieldValueKind.String;
    /// <summary>Value source.</summary>
    public EventFieldSource Source { get; set; }
    /// <summary>Data key, metadata property, or literal constant.</summary>
    public string SourceName { get; set; } = string.Empty;
    /// <summary>Fallback value when the selected source is absent.</summary>
    public string? DefaultValue { get; set; }
}

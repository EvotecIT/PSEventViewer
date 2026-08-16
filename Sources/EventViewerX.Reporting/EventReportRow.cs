namespace EventViewerX.Reporting;

/// <summary>A normalized event row shared by HTML, Excel, email, and transport adapters.</summary>
public sealed class EventReportRow {
    /// <summary>Event timestamp.</summary>
    public DateTime TimeCreated { get; set; }
    /// <summary>Built-in type name or Generic.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Event identifier.</summary>
    public int EventId { get; set; }
    /// <summary>Event record identifier.</summary>
    public long? RecordId { get; set; }
    /// <summary>Provider name.</summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>Original source channel.</summary>
    public string SourceLog { get; set; } = string.Empty;
    /// <summary>Container channel or file.</summary>
    public string ContainerLog { get; set; } = string.Empty;
    /// <summary>Computer that emitted the event.</summary>
    public string SourceComputer { get; set; } = string.Empty;
    /// <summary>Direct target or collector from which the event was read.</summary>
    public string CollectorComputer { get; set; } = string.Empty;
    /// <summary>Level display name.</summary>
    public string Level { get; set; } = string.Empty;
    /// <summary>Rendered provider message.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Type-specific projected values.</summary>
    public IReadOnlyDictionary<string, object?> Values { get; set; } = new Dictionary<string, object?>();

    /// <summary>Flattens common and type-specific fields for table renderers.</summary>
    public IReadOnlyDictionary<string, object?> ToDictionary() {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            [nameof(TimeCreated)] = TimeCreated,
            [nameof(Type)] = Type,
            [nameof(EventId)] = EventId,
            [nameof(RecordId)] = RecordId,
            [nameof(Provider)] = Provider,
            [nameof(SourceLog)] = SourceLog,
            [nameof(ContainerLog)] = ContainerLog,
            [nameof(SourceComputer)] = SourceComputer,
            [nameof(CollectorComputer)] = CollectorComputer,
            [nameof(Level)] = Level,
            [nameof(Message)] = Message
        };
        foreach (KeyValuePair<string, object?> value in Values) {
            if (!result.ContainsKey(value.Key)) {
                result[value.Key] = value.Value;
            }
        }
        return result;
    }
}

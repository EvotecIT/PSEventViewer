namespace EventViewerX.Reporting;

/// <summary>Serializable homogeneous report schema used by storage and transport adapters.</summary>
public sealed class EventReportSectionSchema {
    /// <summary>Stable definition name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Human-friendly section name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Definition purpose.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Generic, built-in typed, or custom definition.</summary>
    public EventReportSectionKind Kind { get; set; }
    /// <summary>Stable homogeneous columns.</summary>
    public IReadOnlyList<EventReportColumnSchema> Columns { get; set; } = Array.Empty<EventReportColumnSchema>();

    /// <summary>Creates a detached schema from a report section.</summary>
    public static EventReportSectionSchema FromSection(EventReportSection section) {
        if (section == null) {
            throw new ArgumentNullException(nameof(section));
        }
        return new EventReportSectionSchema {
            Name = section.Name,
            DisplayName = section.DisplayName,
            Description = section.Description,
            Kind = section.Kind,
            Columns = section.Columns.Select(static column => new EventReportColumnSchema {
                Name = column.Name,
                DisplayName = column.DisplayName,
                ValueTypeName = EventReportColumnSchema.GetStableTypeName(column.ValueType)
            }).ToArray()
        };
    }
}

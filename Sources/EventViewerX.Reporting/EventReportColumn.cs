namespace EventViewerX.Reporting;

/// <summary>Describes one visible column in a report section.</summary>
public sealed class EventReportColumn {
    internal EventReportColumn(string name, string displayName, Type valueType) {
        Name = name;
        DisplayName = displayName;
        ValueType = valueType;
    }

    /// <summary>Stable field name used by report rows.</summary>
    public string Name { get; }
    /// <summary>Human-friendly column heading.</summary>
    public string DisplayName { get; }
    /// <summary>Projected CLR value type.</summary>
    public Type ValueType { get; }
}

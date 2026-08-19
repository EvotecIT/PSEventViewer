namespace EventViewerX;

/// <summary>Describes one strongly typed field emitted by an event definition.</summary>
public sealed class EventFieldDefinition {
    internal EventFieldDefinition(
        string name,
        string displayName,
        Type valueType,
        bool isCommon) {

        Name = name;
        DisplayName = displayName;
        ValueType = valueType;
        IsCommon = isCommon;
    }

    /// <summary>CLR member name on the projected record.</summary>
    public string Name { get; }

    /// <summary>Human-friendly field label.</summary>
    public string DisplayName { get; }

    /// <summary>CLR value type.</summary>
    public Type ValueType { get; }

    /// <summary>Whether the field belongs to every typed event record.</summary>
    public bool IsCommon { get; }
}

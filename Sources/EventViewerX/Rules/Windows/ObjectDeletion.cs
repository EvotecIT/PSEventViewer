namespace EventViewerX.Rules.Windows;

/// <summary>
/// Object deleted
/// 4660: An object was deleted
/// </summary>
public class ObjectDeletion : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 4660 };
    /// <inheritdoc />
    public override string LogName => "Security";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.ObjectDeletion;

    /// <summary>Accepts object deletion events (4660) in the Security log.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }
    /// <summary>Machine where the object was deleted.</summary>
    public string Computer;
    /// <summary>Path of the deleted object.</summary>
    public string Path;
    /// <summary>Account that performed the deletion.</summary>
    public string Who;
    /// <summary>Event timestamp.</summary>
    public DateTime When;

    /// <summary>Initialises an object deletion wrapper from an event record.</summary>
    public ObjectDeletion(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ObjectDeletion";
        Computer = _eventObject.ComputerName;
        Path = _eventObject.GetValueFromDataDictionary("ObjectName");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}


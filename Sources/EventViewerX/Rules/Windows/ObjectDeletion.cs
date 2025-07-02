namespace EventViewerX.Rules.Windows;

/// <summary>
/// Object deleted
/// 4660: An object was deleted
/// </summary>
public class ObjectDeletion : EventRuleBase {
    public override List<int> EventIds => new() { 4660 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ObjectDeletion;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Path;
    public string Who;
    public DateTime When;

    public ObjectDeletion(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ObjectDeletion";
        Computer = _eventObject.ComputerName;
        Path = _eventObject.GetValueFromDataDictionary("ObjectName");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}


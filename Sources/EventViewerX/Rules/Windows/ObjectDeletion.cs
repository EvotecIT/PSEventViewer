namespace EventViewerX.Rules.Windows;

/// <summary>
/// Object deleted
/// 4660: An object was deleted
/// </summary>
public class ObjectDeletion : EventObjectSlim {
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

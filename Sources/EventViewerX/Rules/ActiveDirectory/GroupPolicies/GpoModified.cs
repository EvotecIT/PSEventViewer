namespace EventViewerX.Rules.ActiveDirectory;

public class GpoModified : EventObjectSlim {
    public string Computer;
    public string Action;
    public string GpoName;
    public string AttributeLDAPDisplayName;
    public string AttributeValue;
    public string Who;
    public DateTime When;

    public GpoModified(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "GpoModified";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        GpoName = _eventObject.GetValueFromDataDictionary("ObjectDN");
        AttributeLDAPDisplayName = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
        AttributeValue = _eventObject.GetValueFromDataDictionary("AttributeValue");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}


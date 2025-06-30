namespace EventViewerX.Rules.ActiveDirectory;

public class GpoCreated : EventObjectSlim {
    public string Computer;
    public string Action;
    public string GpoName;
    public string Who;
    public DateTime When;

    public GpoCreated(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "GpoCreated";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        GpoName = _eventObject.GetValueFromDataDictionary("ObjectDN");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}


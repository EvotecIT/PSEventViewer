namespace EventViewerX.Rules.IIS;

/// <summary>
/// IIS site stopped
/// 1005: Website stopped
/// </summary>
public class IISSiteStopped : EventRuleBase {
    public override List<int> EventIds => new() { 1005 };
    public override string LogName => "System";
    public override NamedEvents NamedEvent => NamedEvents.IISSiteStopped;

    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    public string Computer;
    public string SiteName;
    public string User;
    public DateTime When;

    public IISSiteStopped(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "IISSiteStopped";
        Computer = _eventObject.ComputerName;
        SiteName = _eventObject.GetValueFromDataDictionary("SiteName", "Name");
        if (string.IsNullOrEmpty(SiteName)) {
            SiteName = _eventObject.MessageSubject;
        }
        User = _eventObject.GetValueFromDataDictionary("User", "UserName");
        if (string.IsNullOrEmpty(User)) {
            User = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        }
        When = _eventObject.TimeCreated;
    }
}

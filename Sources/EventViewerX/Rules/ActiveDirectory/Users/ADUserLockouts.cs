namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory User Lockouts
/// 4740: A user account was locked out
/// </summary>
public class ADUserLockouts : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ComputerLockoutOn;
    public string UserAffected;
    public string Who;
    public DateTime When;

    public ADUserLockouts(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserLockouts";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        ComputerLockoutOn = _eventObject.GetValueFromDataDictionary("TargetDomainName");

        UserAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
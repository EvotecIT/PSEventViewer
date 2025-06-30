namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// A user account was enabled, disabled, unlocked, password changed, password reset, or deleted
/// 4722: A user account was enabled (this includes computer accounts)
/// 4725: A user account was disabled (this includes computer accounts)
/// 4767: A user account was unlocked
/// 4723: An attempt was made to change an account's password
/// 4724: An attempt was made to reset an account's password
/// 4726: A user account was deleted
/// </summary>
public class ADUserStatus : EventObjectSlim {

    public string Computer;
    public string Action;
    public string Who;
    public DateTime When;
    public string UserAffected;

    public ADUserStatus(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUsersStatus";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        UserAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}

using EventViewerX;
namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory User Logon NTLMv1
/// 4624: An account was successfully logged on (NTLMv1)
/// </summary>
[NamedEvent(NamedEvents.ADUserLogonNTLMv1, "Security", 4624)]
public class ADUserLogonNTLMv1 : ADUserLogon {
    public string LmPackageName;
    public string PackageName;
    public string KeyLength;
    public string ProcessId;
    public string ProcessName;

    public ADUserLogonNTLMv1(EventObject eventObject) : base(eventObject) {
        Type = "ADUserLogonNTLMv1";
        LmPackageName = _eventObject.GetValueFromDataDictionary("LmPackageName");
        PackageName = _eventObject.GetValueFromDataDictionary("AuthenticationPackageName");
        KeyLength = _eventObject.GetValueFromDataDictionary("KeyLength");
        ProcessId = _eventObject.GetValueFromDataDictionary("ProcessId");
        ProcessName = _eventObject.GetValueFromDataDictionary("ProcessName");
    }
    public static EventObjectSlim? TryCreate(EventObject e)
    {
        return e.ValueMatches("LmPackageName", "NTLM V1") ? new ADUserLogonNTLMv1(e) : null;
    }

}

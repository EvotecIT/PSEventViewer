namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory User Logon NTLMv1
/// 4624: An account was successfully logged on (NTLMv1)
/// </summary>
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
}

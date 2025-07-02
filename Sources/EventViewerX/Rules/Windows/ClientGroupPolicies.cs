namespace EventViewerX.Rules.Windows;

/// <summary>
/// Represents client side group policy processing details.
/// </summary>
public class ClientGroupPolicies : EventObjectSlim {
    public string Computer;
    public string Action;
    public string PolicyScope;
    public string ItemName;
    public string PolicyName;
    public string Error;
    public string Who;
    public DateTime When;

    public ClientGroupPolicies(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ClientGroupPolicies";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        PolicyScope = _eventObject.GetValueFromDataDictionary("NoNameA0");
        ItemName = _eventObject.GetValueFromDataDictionary("NoNameA1");
        PolicyName = _eventObject.GetValueFromDataDictionary("NoNameA2", "ExtensionName");
        Error = _eventObject.GetValueFromDataDictionary("NoNameA3", "ErrorDescription");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}

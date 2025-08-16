namespace EventViewerX.Rules.Windows;

/// <summary>
/// Base class for client side group policy processing events.
/// </summary>
public abstract class ClientGroupPoliciesBase : EventRuleBase {
    public string Computer;
    public string Action;
    public string PolicyScope;
    public string ItemName;
    public string PolicyName;
    public string Error;
    public string Who;
    public DateTime When;

    protected ClientGroupPoliciesBase(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = GetType().Name;
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        PolicyScope = _eventObject.GetValueFromDataDictionary("NoNameA0");
        ItemName = _eventObject.GetValueFromDataDictionary("NoNameA1");
        PolicyName = _eventObject.GetValueFromDataDictionary("NoNameA2", "ExtensionName");
        Error = _eventObject.GetValueFromDataDictionary("NoNameA3", "ErrorDescription");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
}


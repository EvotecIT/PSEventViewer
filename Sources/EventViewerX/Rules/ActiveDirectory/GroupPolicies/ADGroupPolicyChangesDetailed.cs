namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Detailed audit information for group policy changes.
/// </summary>
public class ADGroupPolicyChangesDetailed : EventRuleBase {
    public string Computer;
    public string Action;
    public string ObjectClass;
    public string OperationType;
    public string Who;
    public DateTime When;
    public string GpoName;
    public string AttributeLDAPDisplayName;
    public string AttributeValue;
    public override List<int> EventIds => new() { 5136, 5137, 5139, 5141 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADGroupPolicyChangesDetailed;

    public override bool CanHandle(EventObject eventObject) {
        return eventObject.Data.TryGetValue("ObjectClass", out var objectClass) &&
               objectClass == "groupPolicyContainer";
    }

    public ADGroupPolicyChangesDetailed(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupPolicyChangesDetailed";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        ObjectClass = _eventObject.GetValueFromDataDictionary("ObjectClass");
        OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
        GpoName = _eventObject.GetValueFromDataDictionary("ObjectDN");
        AttributeLDAPDisplayName = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
        AttributeValue = _eventObject.GetValueFromDataDictionary("AttributeValue");
    }
}

namespace EventViewerX.Rules.Windows;

/// <summary>
/// System audit policy was changed
/// 4719: System audit policy was changed
/// </summary>
public class AuditPolicyChange : EventObjectSlim {
    public string Computer;
    public string CategoryId;
    public string SubcategoryId;
    public string SubcategoryGuid;
    public string AuditPolicyChanges;
    public string Who;
    public DateTime When;

    public AuditPolicyChange(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "AuditPolicyChange";
        Computer = _eventObject.ComputerName;
        CategoryId = _eventObject.GetValueFromDataDictionary("CategoryId");
        SubcategoryId = _eventObject.GetValueFromDataDictionary("SubcategoryId");
        SubcategoryGuid = _eventObject.GetValueFromDataDictionary("SubcategoryGuid");
        AuditPolicyChanges = _eventObject.GetValueFromDataDictionary("AuditPolicyChanges");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}

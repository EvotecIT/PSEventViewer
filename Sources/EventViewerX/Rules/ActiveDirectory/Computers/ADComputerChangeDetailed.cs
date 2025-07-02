namespace EventViewerX.Rules.ActiveDirectory;

/// Active Directory Computer Change Detailed
/// 5136: A directory service object was modified
/// 5137: A directory service object was created
/// 5139: A directory service object was deleted
/// 5141: A directory service object was moved
public class ADComputerChangeDetailed : EventRuleBase {
    public string Computer;
    public string Action;
    public string ObjectClass;
    public string OperationType;
    public string Who;
    public DateTime When;
    public string ComputerObject; // 'Computer Object'
    public string FieldChanged;
    public string FieldValue;

    //public string ClientDNSName;

    public override List<int> EventIds => new() { 5136, 5137, 5139, 5141 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADComputerChangeDetailed;

    public override bool CanHandle(EventObject eventObject) {
        // Check if this is a computer object change
        return eventObject.Data.TryGetValue("ObjectClass", out var objectClass) &&
               objectClass == "computer";
    }

    public static EventObjectSlim Create(EventObject eventObject) {
        var rule = new ADComputerChangeDetailed(eventObject);
        return rule.CanHandle(eventObject) ? rule : null;
    }

    /// <summary>
    /// Active Directory Computer Change Detailed
    /// </summary>
    /// <param name="eventObject"></param>
    public ADComputerChangeDetailed(EventObject eventObject) : base(eventObject) {
        // common fields
        _eventObject = eventObject;
        Type = "ADComputerChangeDetailed";
        Computer = _eventObject.ComputerName;
        ObjectClass = _eventObject.GetValueFromDataDictionary("ObjectClass");
        Action = _eventObject.MessageSubject;
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
        //
        OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
        ComputerObject = _eventObject.GetValueFromDataDictionary("ObjectDN");
        FieldChanged = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
        FieldValue = _eventObject.GetValueFromDataDictionary("AttributeValue");
        // OverwriteByField logic
        ComputerObject = OverwriteByField(Action, "A directory service object was moved.", ComputerObject, _eventObject.GetValueFromDataDictionary("OldObjectDN"));
        FieldValue = OverwriteByField(Action, "A directory service object was moved.", FieldValue, _eventObject.GetValueFromDataDictionary("NewObjectDN"));

        //ClientDNSName = QueryDnsAsync("1.1.1.1").ConfigureAwait(false).GetAwaiter().GetResult();
    }

    //private static async Task<string> QueryDnsAsync(string clientAddress) {
    //    if (string.IsNullOrEmpty(clientAddress)) {
    //        return null;
    //    }

    //    try {
    //        Settings._logger.WriteVerbose($"Querying DNS for address: {clientAddress}");
    //        var result = await ClientX.QueryDns(clientAddress, DnsRecordType.PTR);
    //        var resolvedNames = string.Join(", ", result.AnswersMinimal.Select(answer => answer.Data));
    //        Settings._logger.WriteVerbose($"Resolved names: {resolvedNames}");
    //        return resolvedNames;
    //    } catch (Exception ex) {
    //        Settings._logger.WriteWarning($"Querying DNS for address: {clientAddress} failed: {ex.Message}");
    //        return null;
    //    }
    //}
}
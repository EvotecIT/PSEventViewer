namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Represents detailed Active Directory computer object changes.
/// Handles events 5136, 5137, 5139 and 5141.
/// </summary>
public class ADComputerChangeDetailed : EventRuleBase {
    /// <summary>Machine where the change occurred.</summary>
    public string Computer;
    /// <summary>Short description of the change.</summary>
    public string Action;
    /// <summary>LDAP object class.</summary>
    public string ObjectClass;
    /// <summary>Translated operation type.</summary>
    public string OperationType;
    /// <summary>User performing the change.</summary>
    public string Who;
    /// <summary>Time of the change.</summary>
    public DateTime When;
    /// <summary>Distinguished name of the computer object.</summary>
    public string ComputerObject; // 'Computer Object'
    /// <summary>Attribute modified.</summary>
    public string FieldChanged;
    /// <summary>Value after modification.</summary>
    public string FieldValue;

    //public string ClientDNSName;

    /// <inheritdoc />
    public override List<int> EventIds => new() { 5136, 5137, 5139, 5141 };
    /// <inheritdoc />
    public override string LogName => "Security";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.ADComputerChangeDetailed;

    public override bool CanHandle(EventObject eventObject) {
        // Check if this is a computer object change
        return eventObject.Data.TryGetValue("ObjectClass", out var objectClass) &&
               objectClass == "computer";
    }

    /// <summary>
    /// Creates a detailed computer change wrapper when the event matches, otherwise returns <c>null</c>.
    /// </summary>
    public static EventObjectSlim Create(EventObject eventObject) {
        var rule = new ADComputerChangeDetailed(eventObject);
        return rule.CanHandle(eventObject) ? rule : null;
    }

    /// <summary>
    /// Active Directory Computer Change Detailed
    /// </summary>
    /// <param name="eventObject">Underlying event record.</param>
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

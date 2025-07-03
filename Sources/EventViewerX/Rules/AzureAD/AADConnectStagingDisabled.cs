namespace EventViewerX.Rules.AzureAD;

/// <summary>
/// Azure AD Connect staging mode disabled
/// 351: Staging mode has been disabled
/// </summary>
public class AADConnectStagingDisabled : EventRuleBase {
    public override List<int> EventIds => new() { 351 };
    public override string LogName => "Application";
    public override NamedEvents NamedEvent => NamedEvents.AADConnectStagingDisabled;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    public string Computer;
    public string Operator;
    public DateTime When;

    public AADConnectStagingDisabled(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "AADConnectStagingDisabled";
        Computer = _eventObject.ComputerName;
        Operator = _eventObject.GetValueFromDataDictionary("Operator");
        if (string.IsNullOrEmpty(Operator)) {
            Operator = _eventObject.GetValueFromDataDictionary(
                "SubjectUserName",
                "SubjectDomainName",
                "\\",
                true);
        }
        When = _eventObject.TimeCreated;
    }
}

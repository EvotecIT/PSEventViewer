namespace EventViewerX.Rules.AzureAD;

/// <summary>
/// Azure AD Connect staging mode enabled
/// 350: Staging mode has been enabled
/// </summary>
public class AADConnectStagingEnabled : EventRuleBase {
    public override List<int> EventIds => new() { 350 };
    public override string LogName => "Application";
    public override NamedEvents NamedEvent => NamedEvents.AADConnectStagingEnabled;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    public string Computer;
    public string Operator;
    public DateTime When;

    public AADConnectStagingEnabled(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "AADConnectStagingEnabled";
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

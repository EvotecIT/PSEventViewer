namespace EventViewerX.Rules.AADConnect;

/// <summary>
/// Azure AD Connect run profile completed
/// 906: The run profile finished executing
/// </summary>
public class AADConnectRunProfile : EventRuleBase {
    public override List<int> EventIds => new() { 906 };
    public override string LogName => "Application";
    public override NamedEvents NamedEvent => NamedEvents.AADConnectRunProfile;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    public string Connector;
    public string RunProfile;
    public DateTime When;

    public AADConnectRunProfile(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "AADConnectRunProfile";
        Connector = _eventObject.GetValueFromDataDictionary("Connector", "Connector Name");
        RunProfile = _eventObject.GetValueFromDataDictionary("RunProfileName", "Run Profile Name", "", false);
        When = _eventObject.TimeCreated;
    }
}

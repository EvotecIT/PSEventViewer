namespace EventViewerX.Rules.Windows;

/// <summary>
/// Client side group policy processing events from the Application log.
/// </summary>
public class ClientGroupPoliciesApplication : ClientGroupPoliciesBase {
    public override List<int> EventIds => new() { 4098 };
    public override string LogName => "Application";
    public override NamedEvents NamedEvent => NamedEvents.ClientGroupPoliciesApplication;

    public ClientGroupPoliciesApplication(EventObject eventObject) : base(eventObject) { }
}


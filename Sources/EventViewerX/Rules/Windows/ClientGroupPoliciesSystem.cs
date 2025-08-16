namespace EventViewerX.Rules.Windows;

/// <summary>
/// Client side group policy processing events from the System log.
/// </summary>
public class ClientGroupPoliciesSystem : ClientGroupPoliciesBase {
    public override List<int> EventIds => new() { 1085 };
    public override string LogName => "System";
    public override NamedEvents NamedEvent => NamedEvents.ClientGroupPoliciesSystem;

    public ClientGroupPoliciesSystem(EventObject eventObject) : base(eventObject) { }
}


namespace EventViewerX.Rules.Windows;

/// <summary>
/// OS Time Change
/// 4616: The system time was changed
/// </summary>
/// <seealso cref="EventViewerX.EventObjectSlim" />
public class OSTimeChange : EventRuleBase {
    public override List<int> EventIds => new() { 4616 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.OSTimeChange;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Action;
    public string ObjectAffected;
    public string PreviousTime;
    public string NewTime;
    public string Who;
    public DateTime When;

    public OSTimeChange(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;

        Type = "OSTimeChange";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        ObjectAffected = _eventObject.MachineName;
        PreviousTime = _eventObject.GetValueFromDataDictionary("PreviousTime");
        NewTime = _eventObject.GetValueFromDataDictionary("NewTime");

        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}

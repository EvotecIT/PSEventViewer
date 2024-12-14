namespace EventViewerX.Rules.Windows;

/// <summary>
/// Windows OS Crash
/// 6008: The previous system shutdown at time on date was unexpected.
/// </summary>
public class OSCrash : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectAffected;
    public string ActionDetails;
    public string ActionDetailsDate;
    public string ActionDetailsTime;
    public string Who;
    public DateTime When;

    public OSCrash(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;

        Type = "OSCrash";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.GetValueFromDataDictionary("EventAction");
        ObjectAffected = _eventObject.MachineName;
        ActionDetails = _eventObject.MessageSubject;
        ActionDetailsDate = _eventObject.GetValueFromDataDictionary("NoNameA1");
        ActionDetailsTime = _eventObject.GetValueFromDataDictionary("NoNameA0");

        When = _eventObject.TimeCreated;
    }
}
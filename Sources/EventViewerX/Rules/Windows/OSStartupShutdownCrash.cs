namespace EventViewerX.Rules.Windows;

/// <summary>
/// OS Startup, Shutdown, Crash
/// 12: Windows is starting up
/// 13: Windows is shutting down
/// 41: The system was not cleanly shut down
/// 4608: Windows is starting up
/// 4621: Administrator recovered system from CrashOnAuditFail
/// 6008: The previous system shutdown at time on date was unexpected.
/// </summary>
public class OSStartupShutdownCrash : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectAffected;
    public string ActionDetails;
    public string ActionDetailsDate;
    public string ActionDetailsTime;
    public string ActionDetailsDateTime;
    public DateTime When;

    public OSStartupShutdownCrash(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;

        Type = "OSStartupShutdownCrash";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.GetValueFromDataDictionary("EventAction");
        ObjectAffected = _eventObject.MachineName;
        ActionDetails = _eventObject.MessageSubject;
        ActionDetailsDate = _eventObject.GetValueFromDataDictionary("NoNameA1");
        ActionDetailsTime = _eventObject.GetValueFromDataDictionary("NoNameA0");
        ActionDetailsDateTime = _eventObject.GetValueFromDataDictionary("ActionDetailsDateTime");

        When = _eventObject.TimeCreated;

        if (_eventObject.Id == 12) {
            Action = "System Start";
        } else if (_eventObject.Id == 13) {
            Action = "System Shutdown";
        } else if (_eventObject.Id == 41) {
            Action = "System Dirty Reboot";
        } else if (_eventObject.Id == 4608) {
            Action = "Windows is starting up";
        } else if (_eventObject.Id == 4621) {
            Action = "Administrator recovered system from CrashOnAuditFail";
        } else if (_eventObject.Id == 6008) {
            Action = "System Crash";
        }

        var startTime = _eventObject.GetValueFromDataDictionary("StartTime");
        if (startTime != null) {
            ActionDetailsDateTime = startTime;
        } else {
            var text = _eventObject.GetValueFromDataDictionary("#text");
            if (text != null) {
                ActionDetailsDateTime = text;
            }
        }
    }
}
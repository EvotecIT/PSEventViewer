﻿namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory Computer Deleted
/// 4743: A computer account was deleted
/// </summary>
public class ADComputerDeleted : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ComputerAffected;
    public string Who;
    public DateTime When;

    public ADComputerDeleted(EventObject eventObject) : base(eventObject) {
        // common fields
        _eventObject = eventObject;
        Type = "ADComputerDeleted";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        ComputerAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
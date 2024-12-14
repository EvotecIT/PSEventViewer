namespace EventViewerX.Rules.ActiveDirectory;

//public enum FailureReason {
//    UnknownUserNameOrBadPassword,
//    AccountRestrictions,
//    AccountLockedOut,
//    AccountExpired,
//    LogonTypeNotGranted,
//    PasswordExpired
//}

// public enum FailureReason1 {
//     // Define failure reasons based on status codes
//     UnknownUserNameOrBadPassword = 0xC000006D,
//     AccountRestrictions = 0xC000006E,
//     AccountLockedOut = 0xC0000234,
//     AccountExpired = 0xC0000193,
//     LogonTypeNotGranted = 0xC000015B,
//     PasswordExpired = 0xC0000071
// }

/// <summary>
/// Active Directory User Logon Failed (NTLM)
///
/// Event ID: 4625
/// </summary>
public class ADUserLogonFailed : EventObjectSlim {
    public string Computer;
    public string Action;
    public string PackageName;
    public string IpAddress;
    public string IpPort;
    //public string WorkstationName;
    public string ObjectAffected;
    public string Who;
    public DateTime When;
    public string LogonProcessName;
    public LogonType? LogonType;
    public StatusCode? Status { get; private set; }
    public SubStatusCode? SubStatus { get; private set; }
    public FailureReason? FailureReason { get; private set; }
    public string LmPackageName;
    public string KeyLength;
    public string ProcessId;
    public string ProcessName;
    public string TransmittedServices;


    public ADUserLogonFailed(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserLogonFailed";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        //Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        Who = _eventObject.GetValueFromDataDictionary("WorkstationName");
        ObjectAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
        IpAddress = _eventObject.GetValueFromDataDictionary("IpAddress");
        IpPort = _eventObject.GetValueFromDataDictionary("IpPort");
        //WorkstationName = _eventObject.GetValueFromDataDictionary("WorkstationName");
        LogonProcessName = _eventObject.GetValueFromDataDictionary("LogonProcessName");
        LogonType = ParseLogonType(_eventObject.GetValueFromDataDictionary("LogonType"));
        // Parse Status, SubStatus, and FailureReason
        Status = ParseStatus(_eventObject.GetValueFromDataDictionary("Status"));
        SubStatus = ParseSubStatus(_eventObject.GetValueFromDataDictionary("SubStatus"));
        FailureReason = ParseFailureReason(_eventObject.GetValueFromDataDictionary("FailureReason"));

        LmPackageName = _eventObject.GetValueFromDataDictionary("LmPackageName");
        KeyLength = _eventObject.GetValueFromDataDictionary("KeyLength");
        ProcessId = _eventObject.GetValueFromDataDictionary("ProcessId");
        ProcessName = _eventObject.GetValueFromDataDictionary("ProcessName");
        TransmittedServices = _eventObject.GetValueFromDataDictionary("TransmittedServices");
        PackageName = _eventObject.GetValueFromDataDictionary("AuthenticationPackageName");
        When = _eventObject.TimeCreated;
    }

    private StatusCode? ParseStatus(string status) {
        if (string.IsNullOrEmpty(status)) return null;

        // Remove "0x" prefix if present and parse as hex
        status = status.ToLowerInvariant().Replace("0x", "");
        if (uint.TryParse(status, System.Globalization.NumberStyles.HexNumber, null, out uint statusCode)) {
            return (StatusCode)statusCode;
        }
        return null;
    }

    private SubStatusCode? ParseSubStatus(string subStatus) {
        if (string.IsNullOrEmpty(subStatus)) return null;

        // Remove "0x" prefix if present and parse as hex
        subStatus = subStatus.ToLowerInvariant().Replace("0x", "");
        if (uint.TryParse(subStatus, System.Globalization.NumberStyles.HexNumber, null, out uint subStatusCode)) {
            return (SubStatusCode)subStatusCode;
        }
        return null;
    }

    private FailureReason? ParseFailureReason(string reason) {
        if (string.IsNullOrEmpty(reason)) return null;

        // Remove "%%" prefix if present and parse as integer
        reason = reason.Replace("%%", "");
        if (int.TryParse(reason, out int reasonCode)) {
            return (FailureReason)reasonCode;
        }
        return null;
    }

    private LogonType? ParseLogonType(string logonType) {
        if (int.TryParse(logonType, out int lt)) {
            return (LogonType)lt;
        }
        return null;
    }

    /// <summary>
    /// Gets a human-readable description of the failure
    /// </summary>
    public string GetFailureDescription() {
        var description = new System.Text.StringBuilder();

        if (Status.HasValue)
            description.AppendLine($"Status: {Status} (0x{(uint)Status:X8})");

        if (SubStatus.HasValue)
            description.AppendLine($"SubStatus: {SubStatus} (0x{(uint)SubStatus:X8})");

        if (FailureReason.HasValue)
            description.AppendLine($"Reason: {FailureReason}");

        return description.ToString().TrimEnd();
    }
}
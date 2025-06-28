namespace EventViewerX;

public class EventObjectSlim {
    public EventObject _eventObject;
    public int EventID; // = _eventObject.Id;
    public long? RecordID; // = _eventObject.RecordId;
    public string GatheredFrom; // = _eventObject.MachineName;
    public string GatheredLogName; // = _eventObject.LogName;
    public string Type;

    private static readonly Dictionary<int, string> uacFlags = new Dictionary<int, string> {
        { 0x0001, "SCRIPT" },
        { 0x0002, "ACCOUNTDISABLE" },
        { 0x0008, "HOMEDIR_REQUIRED" },
        { 0x0010, "LOCKOUT" },
        { 0x0020, "PASSWD_NOTREQD" },
        { 0x0040, "PASSWD_CANT_CHANGE" },
        { 0x0080, "ENCRYPTED_TEXT_PWD_ALLOWED" },
        { 0x0100, "TEMP_DUPLICATE_ACCOUNT" },
        { 0x0200, "NORMAL_ACCOUNT" },
        { 0x0800, "INTERDOMAIN_TRUST_ACCOUNT" },
        { 0x1000, "WORKSTATION_TRUST_ACCOUNT" },
        { 0x2000, "SERVER_TRUST_ACCOUNT" },
        { 0x10000, "DONT_EXPIRE_PASSWORD" },
        { 0x20000, "MNS_LOGON_ACCOUNT" },
        { 0x40000, "SMARTCARD_REQUIRED" },
        { 0x80000, "TRUSTED_FOR_DELEGATION" },
        { 0x100000, "NOT_DELEGATED" },
        { 0x200000, "USE_DES_KEY_ONLY" },
        { 0x400000, "DONT_REQ_PREAUTH" },
        { 0x800000, "PASSWORD_EXPIRED" },
        { 0x1000000, "TRUSTED_TO_AUTH_FOR_DELEGATION" },
        { 0x04000000, "PARTIAL_SECRETS_ACCOUNT" }
    }
  
    private static readonly Dictionary<string, string> OperationTypeLookup = new()
    {
        {"%%14674", "Value Added"},
        {"%%14675", "Value Deleted"},
        {"%%14676", "Unknown"}
    };


    public EventObjectSlim(EventObject eventObject) {
        _eventObject = eventObject;
        EventID = _eventObject.Id;
        RecordID = _eventObject.RecordId;
        GatheredFrom = _eventObject.QueriedMachine;
        GatheredLogName = _eventObject.ContainerLog;
    }

    internal static string ConvertToObjectAffected(EventObject eventObject) {
        if (eventObject.Data.ContainsKey("TargetUserName") && eventObject.Data.ContainsKey("TargetDomainName")) {
            return eventObject.Data["TargetDomainName"] + "\\" + eventObject.Data["TargetUserName"];
        } else if (eventObject.Data.ContainsKey("TargetUserName")) {
            return eventObject.Data["TargetUserName"];
        } else {
            return "";
        }
    }
    internal static string ConvertToSamAccountName(EventObject eventObject) {
        if (eventObject.Data.ContainsKey("SamAccountName")) {
            return eventObject.Data["SamAccountName"];
        } else {
            return "";
        }
    }
    internal string ConvertFromOperationType(string s) {
        if (OperationTypeLookup.ContainsKey(s)) {
            return OperationTypeLookup[s];
        }

        return "Unknown Operation";
    }
    internal static string OverwriteByField(string findField, string expectedValue, string currentValue, string insertValue) {
        //OverwriteByField = [ordered] @{
        //    'User Object' = 'Action', 'A directory service object was moved.', 'OldObjectDN'
        //    'Field Value' = 'Action', 'A directory service object was moved.', 'NewObjectDN'
        //}
        if (findField == expectedValue) {
            return insertValue;
        } else {
            return currentValue;
        }

    }
    internal static string TranslateUacValue(string hexValue) {
        if (hexValue == null || hexValue.Trim() == "-") {
            return "";
        }
        // <Data Name="OldUacValue">0x10</Data>
        // <Data Name="NewUacValue">0x11</Data>
        // <Data Name="UserAccountControl">%%2080</Data>

        // Convert the hexadecimal value to an integer
        int uacValue = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);

        // Use predefined UAC flags dictionary

        // Map each bit in the UAC value to a UAC flag
        List<string> translatedFlags = new List<string>();
        foreach (var flag in uacFlags) {
            if ((uacValue & flag.Key) != 0) {
                translatedFlags.Add(flag.Value);
            }
        }

        // Return the translated UAC flags as a comma-separated string
        return string.Join(", ", translatedFlags);
    }
}
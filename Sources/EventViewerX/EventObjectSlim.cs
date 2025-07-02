using System.Reflection;
using EventViewerX.Rules.ActiveDirectory;
using EventViewerX.Rules.Kerberos;
using EventViewerX.Rules.Logging;
using EventViewerX.Rules.Windows;
using EventViewerX.Rules.CertificateAuthority;
using EventViewerX.Rules.NPS;

namespace EventViewerX;

public class EventObjectSlim {
    public EventObject _eventObject;
    public int EventID; // = _eventObject.Id;
    public long? RecordID; // = _eventObject.RecordId;
    public string GatheredFrom; // = _eventObject.MachineName;
    public string GatheredLogName; // = _eventObject.LogName;
    public string Type;

    private static readonly Dictionary<NamedEvents, Type> _eventRuleTypes = new();
    private static readonly Dictionary<(int EventId, string LogName), List<Type>> _eventHandlers = new();
    private static bool _initialized = false;

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
    };

    static EventObjectSlim() {
        InitializeEventRules();
    }

    /// <summary>
    /// Discovers and registers all event rule types using reflection
    /// </summary>
    private static void InitializeEventRules() {
        if (_initialized) return;

        var assembly = typeof(EventObjectSlim).Assembly;

        // Find all EventRuleBase classes
        var eventRuleTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                   (t.IsSubclassOf(typeof(EventRuleBase)) ||
                    (t.IsSubclassOf(typeof(EventObjectSlim)) && t.GetInterfaces().Contains(typeof(IEventRule)))));

        foreach (var type in eventRuleTypes) {
            RegisterEventRuleType(type);
        }

        _initialized = true;
    }



    /// <summary>
    /// Registers a single event rule type
    /// </summary>
    private static void RegisterEventRuleType(Type ruleType) {
        // For EventRuleBase classes, we get the NamedEvent from the rule itself
        if (ruleType.IsSubclassOf(typeof(EventRuleBase))) {
            try {
                // Create a dummy EventObject to instantiate the rule and get its NamedEvent
                var dummyEventObject = CreateDummyEventObject();
                var constructor = ruleType.GetConstructor(new[] { typeof(EventObject) });
                if (constructor != null) {
                    var instance = (EventRuleBase)constructor.Invoke(new object[] { dummyEventObject });
                    _eventRuleTypes[instance.NamedEvent] = ruleType;
                }
            } catch {
                // If we can't create instance, skip this type
                return;
            }
        } else {
            // Try EventRuleAttribute for legacy support
            var attr = ruleType.GetCustomAttribute<EventRuleAttribute>();
            if (attr != null) {
                _eventRuleTypes[attr.NamedEvent] = ruleType;

                foreach (var eventId in attr.EventIds) {
                    var key = (eventId, attr.LogName);
                    if (!_eventHandlers.ContainsKey(key)) {
                        _eventHandlers[key] = new List<Type>();
                    }
                    _eventHandlers[key].Add(ruleType);
                }
            }
        }
    }

    /// <summary>
    /// Gets all event rule types that can handle the given event
    /// </summary>
    public static List<Type> GetEventHandlers(int eventId, string logName) {
        var key = (eventId, logName);
        return _eventHandlers.TryGetValue(key, out var handlers) ? handlers : new List<Type>();
    }

    /// <summary>
    /// Gets the event rule type for a named event
    /// </summary>
    public static Type GetEventRuleType(NamedEvents namedEvent) {
        return _eventRuleTypes.TryGetValue(namedEvent, out var type) ? type : null;
    }

    /// <summary>
    /// Creates an event rule instance from an EventObject
    /// </summary>
    public static EventObjectSlim CreateEventRule(EventObject eventObject, List<NamedEvents> targetNamedEvents) {
        // Try each target named event to find a matching rule
        foreach (var namedEvent in targetNamedEvents) {
            var ruleType = GetEventRuleType(namedEvent);
            if (ruleType == null) continue;

            try {
                var constructor = ruleType.GetConstructor(new[] { typeof(EventObject) });
                if (constructor == null) continue;

                var instance = (EventObjectSlim)constructor.Invoke(new object[] { eventObject });

                // For EventRuleBase classes, check if they can handle this specific event
                if (instance is IEventRule eventRule) {
                    if (eventRule.CanHandle(eventObject)) {
                        return instance;
                    }
                } else {
                    // For simple rules without conditions, return the instance
                    return instance;
                }
            } catch {
                // Continue to next rule if creation fails
                continue;
            }
        }

        return null;
    }

    private static NamedEvents GetNamedEventForType(Type type) {
        // For EventRuleBase classes, create instance and get NamedEvent
        if (type.IsSubclassOf(typeof(EventRuleBase))) {
            try {
                var dummyEventObject = CreateDummyEventObject();
                var constructor = type.GetConstructor(new[] { typeof(EventObject) });
                if (constructor != null) {
                    var instance = (EventRuleBase)constructor.Invoke(new object[] { dummyEventObject });
                    return instance.NamedEvent;
                }
            } catch {
                // Fall through to exception
            }
        }

        // Try attribute for legacy rules
        var attr = type.GetCustomAttribute<EventRuleAttribute>();
        if (attr != null) {
            return attr.NamedEvent;
        }

        throw new InvalidOperationException($"Type {type.Name} is not properly configured");
    }

    /// <summary>
    /// Gets event IDs and log name for named events using rule classes
    /// </summary>
    public static Dictionary<string, HashSet<int>> GetEventInfoForNamedEvents(List<NamedEvents> namedEvents) {
        var eventInfoDict = new Dictionary<string, HashSet<int>>();

        foreach (var namedEvent in namedEvents) {
            var ruleType = GetEventRuleType(namedEvent);
            if (ruleType == null) continue;

            List<int> ruleEventIds = null;
            string ruleLogName = null;

            // Check if it's an EventRuleBase class
            if (ruleType.IsSubclassOf(typeof(EventRuleBase))) {
                try {
                    // Create a dummy EventObject to instantiate the rule
                    var dummyEventObject = CreateDummyEventObject();
                    var constructor = ruleType.GetConstructor(new[] { typeof(EventObject) });
                    if (constructor != null) {
                        var instance = (EventRuleBase)constructor.Invoke(new object[] { dummyEventObject });
                        ruleEventIds = instance.EventIds;
                        ruleLogName = instance.LogName;
                    }
                } catch {
                    // If we can't create instance, skip
                    continue;
                }
            } else {
                // Try EventRuleAttribute for legacy rules
                var attr = ruleType.GetCustomAttribute<EventRuleAttribute>();
                if (attr != null) {
                    ruleEventIds = attr.EventIds;
                    ruleLogName = attr.LogName;
                }
            }

            if (ruleEventIds != null && ruleLogName != null) {
                if (!eventInfoDict.TryGetValue(ruleLogName, out var eventIdSet)) {
                    eventIdSet = new HashSet<int>();
                    eventInfoDict[ruleLogName] = eventIdSet;
                }

                foreach (var eventId in ruleEventIds) {
                    eventIdSet.Add(eventId);
                }
            }
        }

        return eventInfoDict;
    }

    /// <summary>
    /// Creates a minimal dummy EventObject for getting rule metadata
    /// </summary>
    private static EventObject CreateDummyEventObject() {
        // This is a hack - we need to create a minimal EventObject just to get metadata
        // In a better design, the metadata would be static or available without instantiation
        try {
            // Try to create with minimal data
            return new EventObject(null, "dummy");
        } catch {
            // If that fails, we'll need to handle this differently
            throw new InvalidOperationException("Cannot create dummy EventObject for metadata extraction");
        }
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
        if (eventObject.Data.TryGetValue("TargetUserName", out var targetUserName)) {
            if (eventObject.Data.TryGetValue("TargetDomainName", out var targetDomainName)) {
                return targetDomainName + "\\" + targetUserName;
            }

            return targetUserName;
        }

        return string.Empty;
    }

    internal static string ConvertToSamAccountName(EventObject eventObject) {
        return eventObject.Data.TryGetValue("SamAccountName", out var samAccountName)
            ? samAccountName
            : string.Empty;
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

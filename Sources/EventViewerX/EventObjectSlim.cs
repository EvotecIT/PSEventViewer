using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace EventViewerX;

/// <summary>
/// Lightweight representation of an event used for rule processing.
/// </summary>
public class EventObjectSlim {
    /// <summary>
    /// Reference to the detailed event object.
    /// </summary>
    public EventObject _eventObject = null!;

    /// <summary>
    /// Identifier of the event.
    /// </summary>
    public int EventID; // = _eventObject.Id;

    /// <summary>
    /// Record identifier of the event.
    /// </summary>
    public long? RecordID; // = _eventObject.RecordId;

    /// <summary>
    /// Source machine from which the event was gathered.
    /// </summary>
    public string GatheredFrom = string.Empty; // = _eventObject.MachineName;

    /// <summary>
    /// Log name where the event originated.
    /// </summary>
    public string GatheredLogName = string.Empty; // = _eventObject.LogName;

    /// <summary>
    /// Name of the rule type handling the event.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    private sealed class RuleFactoryRegistration {
        public RuleFactoryRegistration(NamedEvents namedEvent, string logName, IReadOnlyList<int> eventIds,
            Func<EventObject, EventObjectSlim> factory, Func<EventObject, bool>? canHandle, Type? ruleType) {
            NamedEvent = namedEvent;
            LogName = logName;
            EventIds = eventIds;
            Factory = factory;
            CanHandle = canHandle;
            RuleType = ruleType;
        }

        public NamedEvents NamedEvent { get; }
        public string LogName { get; }
        public IReadOnlyList<int> EventIds { get; }
        public Func<EventObject, EventObjectSlim> Factory { get; }
        public Func<EventObject, bool>? CanHandle { get; }
        public Type? RuleType { get; }
    }

    private static readonly Dictionary<NamedEvents, Type> _reflectionRuleTypes = new();
    private static readonly Dictionary<(int EventId, string LogName), List<Type>> _reflectionHandlers = new();

    private static readonly Dictionary<NamedEvents, Type> _explicitRuleTypes = new();
    private static readonly Dictionary<(int EventId, string LogName), List<Type>> _explicitHandlers = new();

    // AOT-friendly path: explicit, delegate-based rule registration.
    private static readonly Dictionary<NamedEvents, RuleFactoryRegistration> _ruleFactories = new();
    private static readonly Dictionary<(int EventId, string LogName), List<RuleFactoryRegistration>> _factoryHandlers = new();

    private static readonly object _initLock = new();
    private static bool _initialized = false;
    private static EventRuleDiscoveryMode _discoveryMode = EventRuleDiscoveryMode.Auto;

    private static readonly Dictionary<int, string> uacFlags = new() {
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

    /// <summary>
    /// Configures how rule discovery works. Call this once at startup (before any queries) for AOT-friendly behavior.
    /// </summary>
    public static void Configure(EventRuleDiscoveryMode mode) {
        lock (_initLock) {
            if (_initialized) {
                throw new InvalidOperationException(
                    "EventObjectSlim has already been initialized. Configure() must be called before first use.");
            }
            _discoveryMode = mode;
        }
    }

    /// <summary>
    /// Registers a rule factory for a named event without relying on reflection.
    /// This enables AOT-friendly ingestion of selected rules.
    /// </summary>
    /// <param name="namedEvent">Named event identifier.</param>
    /// <param name="logName">Windows log name (channel).</param>
    /// <param name="eventIds">Event IDs this rule handles.</param>
    /// <param name="factory">Factory creating a rule instance from an <see cref="EventObject"/>.</param>
    /// <param name="canHandle">Optional predicate to further validate an event before instantiation.</param>
    /// <param name="ruleType">Optional rule type used for legacy APIs returning <see cref="Type"/>.</param>
    public static void RegisterRuleFactory(
        NamedEvents namedEvent,
        string logName,
        IReadOnlyList<int> eventIds,
        Func<EventObject, EventObjectSlim> factory,
        Func<EventObject, bool>? canHandle = null,
        Type? ruleType = null) {
        if (string.IsNullOrWhiteSpace(logName)) {
            throw new ArgumentException("logName cannot be null or whitespace.", nameof(logName));
        }
        if (eventIds is null || eventIds.Count == 0) {
            throw new ArgumentException("eventIds cannot be null or empty.", nameof(eventIds));
        }
        if (factory is null) {
            throw new ArgumentNullException(nameof(factory));
        }

        var normalizedLog = logName.Trim();
        var ids = eventIds.Where(x => x > 0).Distinct().ToArray();
        if (ids.Length == 0) {
            throw new ArgumentException("eventIds must contain at least one positive event id.", nameof(eventIds));
        }

        var reg = new RuleFactoryRegistration(namedEvent, normalizedLog, ids, factory, canHandle, ruleType);
        _ruleFactories[namedEvent] = reg;

        if (ruleType is not null) {
            _explicitRuleTypes[namedEvent] = ruleType;
        }

        foreach (var eventId in ids) {
            var factoryKey = (eventId, normalizedLog);
            if (!_factoryHandlers.TryGetValue(factoryKey, out var factoryList)) {
                factoryList = new List<RuleFactoryRegistration>();
                _factoryHandlers[factoryKey] = factoryList;
            }
            if (!factoryList.Contains(reg)) {
                factoryList.Add(reg);
            }

            if (ruleType is not null) {
                var legacyKey = (eventId, normalizedLog);
                if (!_explicitHandlers.TryGetValue(legacyKey, out var legacyList)) {
                    legacyList = new List<Type>();
                    _explicitHandlers[legacyKey] = legacyList;
                }
                if (!legacyList.Contains(ruleType)) {
                    legacyList.Add(ruleType);
                }
            }
        }
    }

    private static void EnsureInitialized() {
        if (_initialized) {
            return;
        }
        lock (_initLock) {
            if (_initialized) {
                return;
            }
            if (_discoveryMode != EventRuleDiscoveryMode.ExplicitOnly) {
                InitializeEventRulesWithReflection();
            }
            _initialized = true;
        }
    }

    /// <summary>
    /// Discovers and registers all event rule types using reflection (legacy behavior).
    /// </summary>
    private static void InitializeEventRulesWithReflection() {
        var assembly = typeof(EventObjectSlim).Assembly;

        var eventRuleTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                   (t.IsSubclassOf(typeof(EventRuleBase)) ||
                    (t.IsSubclassOf(typeof(EventObjectSlim)) && t.GetInterfaces().Contains(typeof(IEventRule)))));

        foreach (var type in eventRuleTypes) {
            RegisterEventRuleType(type);
        }
    }

    /// <summary>
    /// Registers a single event rule type (reflection-based).
    /// </summary>
    private static void RegisterEventRuleType(Type ruleType) {
        if (ruleType.IsSubclassOf(typeof(EventRuleBase))) {
            try {
#pragma warning disable SYSLIB0050
                var instance = (EventRuleBase)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(ruleType);
#pragma warning restore SYSLIB0050
                _reflectionRuleTypes[instance.NamedEvent] = ruleType;

                foreach (var eventId in instance.EventIds) {
                    var key = (eventId, instance.LogName);
                    if (!_reflectionHandlers.ContainsKey(key)) {
                        _reflectionHandlers[key] = new List<Type>();
                    }
                    _reflectionHandlers[key].Add(ruleType);
                }
            } catch {
                return;
            }
        } else {
            var attr = ruleType.GetCustomAttribute<EventRuleAttribute>();
            if (attr != null) {
                _reflectionRuleTypes[attr.NamedEvent] = ruleType;

                foreach (var eventId in attr.EventIds) {
                    var key = (eventId, attr.LogName);
                    if (!_reflectionHandlers.ContainsKey(key)) {
                        _reflectionHandlers[key] = new List<Type>();
                    }
                    _reflectionHandlers[key].Add(ruleType);
                }
            }
        }
    }

    /// <summary>
    /// Gets all event rule types that can handle the given event.
    /// </summary>
    public static List<Type> GetEventHandlers(int eventId, string logName) {
        var key = (eventId, logName);
        var mode = _discoveryMode;
        EnsureInitialized();

        if (mode == EventRuleDiscoveryMode.ExplicitOnly) {
            return _explicitHandlers.TryGetValue(key, out var explicitHandlers) ? explicitHandlers : new List<Type>();
        }
        if (mode == EventRuleDiscoveryMode.Reflection) {
            return _reflectionHandlers.TryGetValue(key, out var reflectionHandlers) ? reflectionHandlers : new List<Type>();
        }

        var combined = new List<Type>();
        if (_explicitHandlers.TryGetValue(key, out var explicitList)) {
            combined.AddRange(explicitList);
        }
        if (_reflectionHandlers.TryGetValue(key, out var reflectionList)) {
            foreach (var t in reflectionList) {
                if (!combined.Contains(t)) {
                    combined.Add(t);
                }
            }
        }
        return combined;
    }

    /// <summary>
    /// Gets the event rule type for a named event.
    /// </summary>
    public static Type? GetEventRuleType(NamedEvents namedEvent) {
        var mode = _discoveryMode;
        EnsureInitialized();

        if (mode == EventRuleDiscoveryMode.ExplicitOnly) {
            return _explicitRuleTypes.TryGetValue(namedEvent, out var explicitType) ? explicitType : null;
        }
        if (mode == EventRuleDiscoveryMode.Reflection) {
            return _reflectionRuleTypes.TryGetValue(namedEvent, out var reflectionType) ? reflectionType : null;
        }

        return _explicitRuleTypes.TryGetValue(namedEvent, out var type) ? type
            : _reflectionRuleTypes.TryGetValue(namedEvent, out var reflection) ? reflection
            : null;
    }

    /// <summary>
    /// Creates an event rule instance from an <see cref="EventObject"/>.
    /// </summary>
    public static EventObjectSlim? CreateEventRule(EventObject eventObject, List<NamedEvents> targetNamedEvents) {
        var mode = _discoveryMode;
        EnsureInitialized();

        foreach (var namedEvent in targetNamedEvents) {
            if (mode != EventRuleDiscoveryMode.Reflection && _ruleFactories.TryGetValue(namedEvent, out var reg)) {
                try {
                    if (reg.CanHandle != null && !reg.CanHandle(eventObject)) {
                        continue;
                    }

                    var instance = reg.Factory(eventObject);
                    if (instance is IEventRule eventRule) {
                        if (eventRule.CanHandle(eventObject)) {
                            return instance;
                        }
                        continue;
                    }
                    return instance;
                } catch {
                    continue;
                }
            }

            if (mode == EventRuleDiscoveryMode.ExplicitOnly) {
                continue;
            }

            if (!_reflectionRuleTypes.TryGetValue(namedEvent, out var ruleType) || ruleType == null) {
                continue;
            }

            try {
                var constructor = ruleType.GetConstructor(new[] { typeof(EventObject) });
                if (constructor == null) {
                    continue;
                }

                var instance = (EventObjectSlim)constructor.Invoke(new object[] { eventObject });

                if (instance is IEventRule eventRule) {
                    if (eventRule.CanHandle(eventObject)) {
                        return instance;
                    }
                } else {
                    return instance;
                }
            } catch {
                continue;
            }
        }

        return null;
    }

    private static NamedEvents GetNamedEventForType(Type type) {
        if (type.IsSubclassOf(typeof(EventRuleBase))) {
            try {
#pragma warning disable SYSLIB0050
                var instance = (EventRuleBase)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
#pragma warning restore SYSLIB0050
                return instance.NamedEvent;
            } catch {
            }
        }

        var attr = type.GetCustomAttribute<EventRuleAttribute>();
        if (attr != null) {
            return attr.NamedEvent;
        }

        throw new InvalidOperationException($"Type {type.Name} is not properly configured");
    }

    /// <summary>
    /// Gets event IDs and log name for named events using rule classes.
    /// </summary>
    public static Dictionary<string, HashSet<int>> GetEventInfoForNamedEvents(List<NamedEvents> namedEvents) {
        var mode = _discoveryMode;
        EnsureInitialized();

        var eventInfoDict = new Dictionary<string, HashSet<int>>();

        foreach (var namedEvent in namedEvents) {
            if (mode != EventRuleDiscoveryMode.Reflection && _ruleFactories.TryGetValue(namedEvent, out var reg)) {
                if (!eventInfoDict.TryGetValue(reg.LogName, out var idSet)) {
                    idSet = new HashSet<int>();
                    eventInfoDict[reg.LogName] = idSet;
                }
                foreach (var id in reg.EventIds) {
                    idSet.Add(id);
                }
                continue;
            }

            if (mode == EventRuleDiscoveryMode.ExplicitOnly) {
                continue;
            }

            if (!_reflectionRuleTypes.TryGetValue(namedEvent, out var ruleType) || ruleType == null) {
                continue;
            }

            List<int>? ruleEventIds = null;
            string? ruleLogName = null;

            if (ruleType.IsSubclassOf(typeof(EventRuleBase))) {
                try {
#pragma warning disable SYSLIB0050
                    var instance = (EventRuleBase)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(ruleType);
#pragma warning restore SYSLIB0050
                    ruleEventIds = instance.EventIds;
                    ruleLogName = instance.LogName;
                } catch {
                    continue;
                }
            } else {
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

    private static readonly Dictionary<string, string> OperationTypeLookup = new() {
        { "%%14674", "Value Added" },
        { "%%14675", "Value Deleted" },
        { "%%14676", "Unknown" }
    };

    /// <summary>
    /// Creates a lightweight projection of an <see cref="EventObject"/> for rule processing and serialization.
    /// </summary>
    /// <param name="eventObject">Full event wrapper to down-sample.</param>
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
        if (findField == expectedValue) {
            return insertValue;
        }
        return currentValue;
    }

    internal static string TranslateUacValue(string hexValue) {
        if (hexValue == null || hexValue.Trim() == "-") {
            return "";
        }

        var uacValue = int.Parse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        var translatedFlags = new List<string>();
        foreach (var flag in uacFlags) {
            if ((uacValue & flag.Key) != 0) {
                translatedFlags.Add(flag.Value);
            }
        }

        return string.Join(", ", translatedFlags);
    }
}

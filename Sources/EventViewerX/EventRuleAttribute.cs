using System;
using System.Collections.Generic;

namespace EventViewerX;

/// <summary>
/// Attribute to define event rule metadata for simple cases
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class EventRuleAttribute : Attribute {
    public List<int> EventIds { get; }
    public string LogName { get; }
    public NamedEvents NamedEvent { get; }

    public EventRuleAttribute(NamedEvents namedEvent, string logName, params int[] eventIds) {
        NamedEvent = namedEvent;
        LogName = logName;
        EventIds = new List<int>(eventIds);
    }
}

using System;

namespace EventViewerX;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
internal sealed class NamedEventAttribute : Attribute
{
    public NamedEvents NamedEvent { get; }
    public int[] EventIds { get; }
    public string LogName { get; }

    public NamedEventAttribute(NamedEvents namedEvent, string logName, params int[] eventIds)
    {
        NamedEvent = namedEvent;
        LogName = logName;
        EventIds = eventIds ?? Array.Empty<int>();
    }
}

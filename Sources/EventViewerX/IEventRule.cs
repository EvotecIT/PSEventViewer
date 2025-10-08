using System;
using System.Collections.Generic;

namespace EventViewerX;

/// <summary>
/// Interface for all event rule classes that defines required metadata
/// </summary>
public interface IEventRule {
    /// <summary>
    /// Gets the event IDs this rule handles
    /// </summary>
    List<int> EventIds { get; }

    /// <summary>
    /// Gets the log name where these events are found
    /// </summary>
    string LogName { get; }

    /// <summary>
    /// Gets the named event type this rule represents
    /// </summary>
    NamedEvents NamedEvent { get; }

    /// <summary>
    /// Determines if this rule can handle the given event object
    /// </summary>
    /// <param name="eventObject">The event object to evaluate</param>
    /// <returns>True if this rule can process the event, false otherwise</returns>
    bool CanHandle(EventObject eventObject);
}

/// <summary>
/// Base class for event rules with metadata
/// </summary>
public abstract class EventRuleBase : EventObjectSlim, IEventRule {
    public abstract List<int> EventIds { get; }
    public abstract string LogName { get; }
    public abstract NamedEvents NamedEvent { get; }

    protected EventRuleBase(EventObject eventObject) : base(eventObject) {
    }

    public abstract bool CanHandle(EventObject eventObject);

    /// <summary>
    /// Creates an instance of the event rule from an EventObject
    /// </summary>
    /// <param name="eventObject">The event object to process</param>
    /// <returns>An instance of the event rule or null if it cannot be processed</returns>
    public static EventObjectSlim? TryCreate<T>(EventObject eventObject) where T : EventRuleBase, new() {
        var instance = new T();
        return instance.CanHandle(eventObject) ? (EventObjectSlim?)Activator.CreateInstance(typeof(T), eventObject) : null;
    }
}

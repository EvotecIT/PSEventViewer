using System;
using System.Management.Automation;
using EventViewerX;

namespace PSEventViewer;

internal sealed class PowerShellWatcherEventArgs : EventArgs {
    internal PowerShellWatcherEventArgs(EventObject eventObject) {
        EventObject = eventObject;
    }

    /// <summary>The detached event snapshot delivered to the PowerShell action.</summary>
    public EventObject EventObject { get; }
}

internal sealed class PowerShellWatcherEventBridge {
    internal static ScriptBlock ActionScript { get; } = ScriptBlock.Create(
        "$EventArgs.EventObject | ForEach-Object -Process $Event.MessageData");

    /// <summary>Raised when the native event-log callback publishes a detached event snapshot.</summary>
    public event EventHandler<PowerShellWatcherEventArgs>? EventReceived;

    internal void Publish(EventObject eventObject) {
        EventReceived?.Invoke(this, new PowerShellWatcherEventArgs(eventObject));
    }
}

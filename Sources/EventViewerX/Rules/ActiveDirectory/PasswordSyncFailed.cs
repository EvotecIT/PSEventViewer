using System;
using System.Collections.Generic;

namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Password synchronization failed
/// Event ID 611: Failed password synchronization
/// </summary>
public class PasswordSyncFailed : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 611 };

    /// <inheritdoc />
    public override string LogName => "Application";

    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.AADConnectPasswordSyncFailed;

    /// <summary>Accepts all password sync failure events.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    /// <summary>Server running Azure AD Connect.</summary>
    public string Computer;

    /// <summary>User whose password failed to sync.</summary>
    public string User;

    /// <summary>Error code associated with the failure.</summary>
    public string Error;

    /// <summary>Timestamp of the failure.</summary>
    public DateTime When;

    /// <summary>Initialises a password sync failure wrapper from an event record.</summary>
    public PasswordSyncFailed(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "AADConnectPasswordSyncFailed";
        Computer = _eventObject.ComputerName;
        User = _eventObject.GetValueFromDataDictionary("User", "AccountName");
        Error = _eventObject.GetValueFromDataDictionary("ErrorCode", "FailureCode");
        When = _eventObject.TimeCreated;
        if (string.IsNullOrEmpty(User)) {
            ParseMessage(_eventObject.Message);
        }
    }

    private void ParseMessage(string message) {
        if (string.IsNullOrEmpty(message)) return;
        var match = System.Text.RegularExpressions.Regex.Match(message, "user:?\\s*(?<user>[^\\s'\\\"]+)");
        if (match.Success) {
            User = match.Groups["user"].Value;
        }
    }
}

using System;
using System.Collections.Generic;

namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Password synchronization failed
/// Event ID 611: Failed password synchronization
/// </summary>
public class PasswordSyncFailed : EventRuleBase {
    public override List<int> EventIds => new() { 611 };
    public override string LogName => "Application";
    public override NamedEvents NamedEvent => NamedEvents.PasswordSyncFailed;

    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    public string Computer;
    public string User;
    public string Error;
    public DateTime When;

    public PasswordSyncFailed(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "PasswordSyncFailed";
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

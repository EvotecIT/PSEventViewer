namespace EventViewerX.Rules.Windows;

/// <summary>
/// Windows Update installation failure
/// 20: Installation Failure
/// </summary>
public class WindowsUpdateFailure : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 20 };
    /// <inheritdoc />
    public override string LogName => "Setup";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.WindowsUpdateFailure;

    /// <summary>Accepts update failure events from the Windows Update Client provider.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return RuleHelpers.IsProvider(eventObject, "Microsoft-Windows-WindowsUpdateClient");
    }
    /// <summary>Computer where the update failed.</summary>
    public string Computer;
    /// <summary>KB article number of the update.</summary>
    public string KB;
    /// <summary>Reason of the failure.</summary>
    public string Reason;
    /// <summary>Time the event occurred.</summary>
    public DateTime When;

    /// <summary>Initialises a Windows Update failure wrapper from an event record.</summary>
    public WindowsUpdateFailure(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "WindowsUpdateFailure";
        Computer = _eventObject.ComputerName;
        var title = _eventObject.GetValueFromDataDictionary("UpdateTitle", "Title");
        if (string.IsNullOrEmpty(title)) {
            title = _eventObject.Message;
        }
        var kbMatch = System.Text.RegularExpressions.Regex.Match(title ?? string.Empty, @"KB\d{6,7}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        KB = kbMatch.Success ? kbMatch.Value : string.Empty;
        Reason = _eventObject.GetValueFromDataDictionary("ErrorDescription", "Message");
        if (string.IsNullOrEmpty(Reason)) {
            Reason = _eventObject.GetValueFromDataDictionary("ErrorCode", "ResultCode");
        }
        When = _eventObject.TimeCreated;
    }
}


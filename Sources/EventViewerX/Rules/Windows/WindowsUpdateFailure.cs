namespace EventViewerX.Rules.Windows;

/// <summary>
/// Windows Update installation failure
/// 20: Installation Failure
/// </summary>
public class WindowsUpdateFailure : EventObjectSlim {
    public string Computer;
    public string KB;
    public string Reason;
    public DateTime When;

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

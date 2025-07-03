namespace EventViewerX.Rules.Windows;

/// <summary>
/// Sync process ended event
/// 907: Synchronization completed
/// </summary>
public class SyncCompleted : EventRuleBase {
    public override List<int> EventIds => new() { 907 };
    public override string LogName => "Application";
    public override NamedEvents NamedEvent => NamedEvents.SyncCompleted;

    public override bool CanHandle(EventObject eventObject) => true;

    public string Computer;
    public DateTime When;
    public Dictionary<string, int> Statistics = new();

    public SyncCompleted(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "SyncCompleted";
        Computer = _eventObject.ComputerName;
        When = _eventObject.TimeCreated;

        var matches = System.Text.RegularExpressions.Regex.Matches(_eventObject.Message ?? string.Empty, @"(\w+):\s*(\d+)");
        foreach (System.Text.RegularExpressions.Match match in matches) {
            var key = match.Groups[1].Value;
            if (int.TryParse(match.Groups[2].Value, out var val)) {
                Statistics[key] = val;
            }
        }
    }
}

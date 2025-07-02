namespace EventViewerX.Rules.Exchange;

/// <summary>
/// Exchange mailbox database mounted successfully
/// 9526: The Microsoft Exchange Information Store service successfully mounted the database
/// </summary>
public class ExchangeDatabaseMounted : EventRuleBase {
    public override List<int> EventIds => new() { 9526 };
    public override string LogName => "Application";
    public override NamedEvents NamedEvent => NamedEvents.ExchangeDatabaseMounted;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    public string Computer;
    public string MailboxDatabase;
    public DateTime When;

    public ExchangeDatabaseMounted(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ExchangeDatabaseMounted";
        Computer = _eventObject.ComputerName;
        MailboxDatabase = _eventObject.GetValueFromDataDictionary("Database", "Mailbox Database");
        if (string.IsNullOrEmpty(MailboxDatabase)) {
            var match = System.Text.RegularExpressions.Regex.Match(_eventObject.Message ?? string.Empty,
                "database '(?<db>[^']+)'", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            MailboxDatabase = match.Success ? match.Groups["db"].Value : string.Empty;
        }
        When = _eventObject.TimeCreated;
    }
}

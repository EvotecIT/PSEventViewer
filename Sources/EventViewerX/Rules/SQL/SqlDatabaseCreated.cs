namespace EventViewerX.Rules.SQL;

/// <summary>
/// SQL Server database created
/// 17137: Starting up database
/// 1802: CREATE DATABASE succeeded
/// </summary>
public class SqlDatabaseCreated : EventRuleBase {
    public override List<int> EventIds => new() { 17137, 1802 };
    public override string LogName => "Application";
    public override NamedEvents NamedEvent => NamedEvents.SqlDatabaseCreated;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    public string Computer;
    public string DatabaseName;
    public DateTime When;

    public SqlDatabaseCreated(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "SqlDatabaseCreated";
        Computer = _eventObject.ComputerName;
        DatabaseName = _eventObject.GetValueFromDataDictionary("Database", "Database Name");
        if (string.IsNullOrEmpty(DatabaseName)) {
            var match = System.Text.RegularExpressions.Regex.Match(_eventObject.Message ?? string.Empty,
                "database ['\"]?(?<db>[^'\"]+)['\"]?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            DatabaseName = match.Success ? match.Groups["db"].Value : string.Empty;
        }
        When = _eventObject.TimeCreated;
    }
}

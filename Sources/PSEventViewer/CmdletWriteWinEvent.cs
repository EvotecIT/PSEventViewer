namespace PSEventViewer;

/// <summary>
/// Writes custom events to Windows Event Logs for testing, debugging, or application logging.
/// Supports creating events with custom data, providers, and event IDs.
/// </summary>
[Cmdlet(VerbsCommunications.Write, "EVXEntry")]
[Alias("Write-EventViewerXEntry", "Write-WinEvent")]
[OutputType(typeof(bool))]
public sealed class CmdletWriteEVXEntry : AsyncPSCmdlet {
    [Alias("ComputerName", "ServerName")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public string MachineName;

    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "GenericEvents")]
    public string LogName;

    [Alias("Source", "Provider")]
    [Parameter(Mandatory = true, ParameterSetName = "GenericEvents")]
    public string ProviderName;

    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public int Category;

    [Alias("EntryType")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public System.Diagnostics.EventLogEntryType EventLogEntryType = System.Diagnostics.EventLogEntryType.Information;

    [Alias("Id")]
    [Parameter(Mandatory = true, ParameterSetName = "GenericEvents")]
    public int EventId;

    [Parameter(Mandatory = true, ParameterSetName = "GenericEvents")]
    public string Message;

    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public string[] AdditionalFields;

    private ActionPreference errorAction;

    protected override Task BeginProcessingAsync() {
        // Get the error action preference as user requested
        // It first sets the error action to the default error action preference
        // If the user has specified the error action, it will set the error action to the user specified error action
        errorAction = (ActionPreference)this.SessionState.PSVariable.GetValue("ErrorActionPreference");
        if (this.MyInvocation.BoundParameters.ContainsKey("ErrorAction")) {
            string errorActionString = this.MyInvocation.BoundParameters["ErrorAction"].ToString();
            if (Enum.TryParse(errorActionString, true, out ActionPreference actionPreference)) {
                errorAction = actionPreference;
            }
        }

        // Initialize the logger to be able to see verbose, warning, debug, error, progress, and information messages.
        var internalLogger = new InternalLogger();
        var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
        LoggingMessages.Logger = internalLogger;
        var searchEvents = new SearchEvents(internalLogger);
        return Task.CompletedTask;
    }
    protected override Task ProcessRecordAsync() {
        SearchEvents.WriteEvent(ProviderName, LogName, Message, EventLogEntryType, Category, EventId, MachineName, AdditionalFields);
        return Task.CompletedTask;
    }
}
namespace PSEventViewer;

/// <summary>
/// Writes custom events to Windows Event Logs for testing, debugging, or application logging.
/// Supports creating events with custom data, providers, and event IDs.
/// Only the "GenericEvents" parameter set is currently supported.
/// </summary>
[Cmdlet(VerbsCommunications.Write, "EVXEntry")]
[Alias("Write-EventViewerXEntry", "Write-WinEvent")]
[OutputType(typeof(bool))]
public sealed class CmdletWriteEVXEntry : AsyncPSCmdlet {
    /// <summary>
    /// Target computer to write the event to.
    /// </summary>
    [Alias("ComputerName", "ServerName")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public string MachineName;

    /// <summary>
    /// Name of the event log where the entry will be written.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "GenericEvents")]
    public string LogName;

    /// <summary>
    /// Name of the provider that writes the entry.
    /// </summary>
    [Alias("Source", "Provider")]
    [Parameter(Mandatory = true, ParameterSetName = "GenericEvents")]
    public string ProviderName;

    /// <summary>
    /// Category for the event entry.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public int Category;

    /// <summary>
    /// Type of the event log entry.
    /// </summary>
    [Alias("EntryType")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public System.Diagnostics.EventLogEntryType EventLogEntryType = System.Diagnostics.EventLogEntryType.Information;

    /// <summary>
    /// Identifier for the event entry.
    /// </summary>
    [Alias("Id")]
    [Parameter(Mandatory = true, ParameterSetName = "GenericEvents")]
    public int EventId;

    /// <summary>
    /// Message for the event entry.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "GenericEvents")]
    public string Message;

    /// <summary>
    /// Additional custom fields to include with the event.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public string[] AdditionalFields;

    private ActionPreference errorAction;

    /// <summary>
    /// Initializes processing and reads error preferences.
    /// </summary>
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
    /// <summary>
    /// Writes the event using <see cref="SearchEvents"/>.
    /// </summary>
    protected override Task ProcessRecordAsync() {
        SearchEvents.WriteEvent(ProviderName, LogName, Message, EventLogEntryType, Category, EventId, MachineName, AdditionalFields);
        return Task.CompletedTask;
    }
}
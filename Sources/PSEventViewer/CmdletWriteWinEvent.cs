namespace PSEventViewer;

[Alias("Write-Event")]
[Cmdlet(VerbsCommunications.Write, "WinEvent")]
public sealed class CmdletWriteWinEvent : AsyncPSCmdlet {
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

    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public System.Diagnostics.EventLogEntryType EventLogEntryType = System.Diagnostics.EventLogEntryType.Information;

    [Alias("Id")]
    [Parameter(Mandatory = true, ParameterSetName = "GenericEvents")]
    public int EventId;

    [Parameter(Mandatory = true, ParameterSetName = "GenericEvents")]
    public string Message;

    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public string[] AdditionalFields;


    protected override Task BeginProcessingAsync() {
        // Initialize the logger to be able to see verbose, warning, debug, error, progress, and information messages.
        var internalLogger = new InternalLogger(false);
        var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
        var searchEvents = new SearchEvents(internalLogger);
        return Task.CompletedTask;
    }
    protected override Task ProcessRecordAsync() {
        SearchEvents.WriteEvent(ProviderName, LogName, Message, EventLogEntryType, Category, EventId, MachineName, AdditionalFields);
        return Task.CompletedTask;
    }
}
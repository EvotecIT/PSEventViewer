namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Writes custom events to Windows Event Logs for testing, debugging, or application logging.</para>
/// <para type="description">Writes through ClassicEventLogManager. A normal write never performs an implicit administrative source registration; use CreateSource explicitly when that behavior is intended.</para>
/// </summary>
/// <example>
///   <summary>Write informational message</summary>
///   <code>Write-EVXEntry -LogName Application -ProviderName MyApp -EventId 1000 -Message "Startup complete"</code>
///   <para>Creates an information entry in Application using provider MyApp.</para>
/// </example>
/// <example>
///   <summary>Write warning to remote server</summary>
///   <code>Write-EVXEntry -MachineName SRV01 -LogName Application -ProviderName MyApp -EventId 2001 -EventLogEntryType Warning -Message "Cache warming delayed"</code>
///   <para>Targets a remote machine and sets the entry type to Warning.</para>
/// </example>
/// <example>
///   <summary>Include custom fields</summary>
///   <code>Write-EVXEntry -LogName Application -ProviderName MyApp -EventId 3001 -Message "User action" -AdditionalFields User:alice Action:Delete</code>
///   <para>Stores extra key/value data alongside the event for later filtering.</para>
/// </example>
/// <example>
///   <summary>Write error with category</summary>
///   <code>Write-EVXEntry -LogName Application -ProviderName MyApp -EventId 4001 -Category 42 -EventLogEntryType Error -Message "Unhandled exception"</code>
///   <para>Records an error and sets a custom category value.</para>
/// </example>
[Cmdlet(
    VerbsCommunications.Write,
    "EVXEntry",
    SupportsShouldProcess = true)]
public sealed class CmdletWriteEVXEntry : AsyncPSCmdlet {
    /// <summary>
    /// Target computer to write the event to.
    /// </summary>
    [Alias("ComputerName", "ServerName")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public string? MachineName { get; set; }

    /// <summary>
    /// Name of the event log where the entry will be written.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "RecordId")]
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = "GenericEvents")]
    public string LogName { get; set; } = null!;

    /// <summary>
    /// Name of the provider that writes the entry.
    /// </summary>
    [Alias("Source", "Provider")]
    [Parameter(Mandatory = true, ParameterSetName = "GenericEvents")]
    public string ProviderName { get; set; } = null!;

    /// <summary>
    /// Category for the event entry.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public int Category { get; set; }

    /// <summary>
    /// Type of the event log entry.
    /// </summary>
    [Alias("EntryType")]
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public System.Diagnostics.EventLogEntryType EventLogEntryType { get; set; } = System.Diagnostics.EventLogEntryType.Information;

    /// <summary>
    /// Identifier for the event entry.
    /// </summary>
    [Alias("Id")]
    [Parameter(Mandatory = true, ParameterSetName = "GenericEvents")]
    public int EventId { get; set; }

    /// <summary>
    /// Message for the event entry.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "GenericEvents")]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Additional custom fields to include with the event.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public string[]? AdditionalFields { get; set; }

    /// <summary>
    /// Explicitly registers a missing source before writing. Source registration normally requires administrative rights.
    /// </summary>
    [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
    public SwitchParameter CreateSource { get; set; }

    /// <summary>
    /// Initializes processing and reads error preferences.
    /// </summary>
    protected override Task BeginProcessingAsync() {
        // Initialize the logger to be able to see verbose, warning, debug, error, progress, and information messages.
        var internalLogger = new InternalLogger();
        var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
        Settings.Logger = internalLogger;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Writes the event using <see cref="ClassicEventLogManager"/>.
    /// </summary>
    protected override Task ProcessRecordAsync() {
        try {
            string target = string.IsNullOrWhiteSpace(MachineName)
                ? $"{LogName}/{ProviderName}"
                : $"{MachineName}/{LogName}/{ProviderName}";
            if (!ShouldProcess(target, $"Write event {EventId}")) {
                return Task.CompletedTask;
            }
            ClassicEventLogManager.Write(
                new ClassicEventWriteRequest {
                    SourceName = ProviderName,
                    LogName = LogName,
                    Message = Message,
                    EntryType = EventLogEntryType,
                    Category = Category,
                    EventId = EventId,
                    MachineName = MachineName,
                    ReplacementStrings = AdditionalFields,
                    CreateSourceIfMissing =
                        CreateSource.IsPresent
                });
        } catch (Exception ex) {
            WriteError(new ErrorRecord(ex, "WriteEventFailed", ErrorCategory.WriteError, this));
        }

        return Task.CompletedTask;
    }
}

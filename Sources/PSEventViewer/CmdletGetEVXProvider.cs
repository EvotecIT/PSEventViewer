using System.Globalization;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Returns detached Windows Event Log provider metadata.</para>
/// <para type="description">Supports local and remote provider discovery, wildcard names, deterministic culture, linked channels, levels, tasks, opcodes, keywords, and optional event definitions.</para>
/// </summary>
/// <example>
///   <summary>Inspect providers and linked channels</summary>
///   <code>Get-EVXProvider -Name '*Security*' | Select-Object Name, LogLinks</code>
///   <para>Returns reusable detached metadata rather than disposable ProviderMetadata handles.</para>
/// </example>
/// <example>
///   <summary>Retain the historical string-only output</summary>
///   <code>Get-EVXProvider -Name '*IIS*' -NameOnly</code>
///   <para>Outputs only provider names for scripts that need strings.</para>
/// </example>
[Cmdlet(VerbsCommon.Get, "EVXProvider")]
[OutputType(typeof(EventProviderMetadataSnapshot))]
[OutputType(typeof(EventProviderCatalogResult))]
[OutputType(typeof(string))]
public sealed class CmdletGetEVXProvider : AsyncPSCmdlet {
    /// <summary>Provider names or wildcard patterns.</summary>
    [Parameter(Position = 0)]
    public string[] Name { get; set; } = new[] { "*" };

    /// <summary>Remote computer name. Omit for the local computer.</summary>
    [Parameter]
    [Alias("ComputerName", "ServerName")]
    public string? MachineName { get; set; }

    /// <summary>Credentials for a remote provider catalog.</summary>
    [Credential]
    [Parameter]
    public PSCredential? Credential { get; set; }

    /// <summary>Authentication package for the remote session.</summary>
    [Parameter]
    public EventLogAuthentication Authentication { get; set; }

    /// <summary>Maximum time for remote RPC preflight and session establishment.</summary>
    [Parameter]
    [ValidateRange(1, int.MaxValue)]
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>Culture used for provider display metadata.</summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>Includes all provider event definitions and templates.</summary>
    [Parameter]
    public SwitchParameter IncludeEvents { get; set; }

    /// <summary>Returns provider names instead of metadata snapshots.</summary>
    [Parameter]
    public SwitchParameter NameOnly { get; set; }

    /// <summary>Returns one success/failure result for every matching provider.</summary>
    [Parameter]
    public SwitchParameter AsResult { get; set; }

    /// <inheritdoc />
    protected override Task ProcessRecordAsync() {
        if (NameOnly && AsResult) {
            throw new PSArgumentException(
                "NameOnly and AsResult are mutually exclusive.");
        }
        if (NameOnly && IncludeEvents) {
            throw new PSArgumentException(
                "IncludeEvents cannot be combined with NameOnly.");
        }
        var query = new EventLogCatalogQuery {
            MachineName = MachineName,
            Credential = Credential?.GetNetworkCredential(),
            Authentication = Authentication,
            ConnectionTimeoutMilliseconds = TimeoutMs,
            Culture = Culture,
            IncludeEvents = IncludeEvents
        };
        if (NameOnly) {
            foreach (string providerName in
                     EventLogCatalog.GetProviderNames(
                         query,
                         Name,
                         CancelToken)) {
                WriteObject(providerName);
            }
            return Task.CompletedTask;
        }
        foreach (EventProviderCatalogResult result in
                 EventLogCatalog.GetProviders(
                     query,
                     Name,
                     CancelToken)) {
            CancelToken.ThrowIfCancellationRequested();
            if (AsResult) {
                WriteObject(result);
                continue;
            }
            if (!result.Success) {
                WriteError(new ErrorRecord(
                    result.Exception!,
                    "EVXProviderMetadataFailed",
                    ErrorCategory.ReadError,
                    result.ProviderName));
                continue;
            }
            WriteObject(result.Provider);
        }
        return Task.CompletedTask;
    }
}

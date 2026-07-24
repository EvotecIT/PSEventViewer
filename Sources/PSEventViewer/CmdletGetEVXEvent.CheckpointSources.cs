using System.Collections;
using System.Net;

namespace PSEventViewer;

public sealed partial class CmdletGetEVXEvent {
    private IReadOnlyList<string> GetCheckpointSources(
        out bool usesFiles) {

        switch (ParameterSetName) {
            case "PathEvents":
                usesFiles = true;
                return ExpandFilePaths(Path, nameof(Path));
            case "FilterHashtableEvents":
                Hashtable[] hashtables =
                    GetFilterHashtables();
                PowerShellEventFilterBinding[] bindings =
                    hashtables
                        .Select(PowerShellEventFilterAdapter.Bind)
                        .ToArray();
                bool hasChannels = bindings.Any(
                    static binding =>
                        binding.UsesChannels ||
                        binding.ProviderOnly);
                bool hasFiles = bindings.Any(
                    static binding => binding.UsesFiles);
                if (UsesCheckpoint &&
                    (bindings.Length != 1 ||
                     (hasChannels && hasFiles))) {
                    throw new PSArgumentException(
                        "RecordIdFile requires one FilterHashtable targeting only channels or only files. Multiple or mixed sources can have unrelated monotonic record sequences.");
                }
                usesFiles = hasFiles && !hasChannels;
                var filterSources = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
                foreach (PowerShellEventFilterBinding binding in
                         bindings) {
                    IEnumerable<string> sources;
                    if (binding.ProviderOnly) {
                        sources = ResolveCheckpointProviderChannels(
                            binding.Select.ProviderNames ??
                            Array.Empty<string>());
                    } else {
                        sources = binding.UsesFiles
                            ? ExpandFilePaths(
                                binding.Paths,
                                nameof(FilterHashtable))
                            : Array.Empty<string>();
                        foreach (string channel in
                                 ExpandCheckpointChannels(
                                     binding.LogNames)) {
                            filterSources.Add(channel);
                        }
                    }
                    foreach (string source in sources) {
                        filterSources.Add(source);
                    }
                }
                return filterSources
                    .OrderBy(
                        static source => source,
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            case "NamedEvents":
                usesFiles = false;
                IReadOnlyList<string> namedSources = EventObjectSlim
                    .GetEventInfoForNamedEvents(Type.ToList())
                    .Keys
                    .ToArray();
                return LogName.Length == 0
                    ? namedSources
                    : namedSources
                        .Where(source => string.Equals(
                            source,
                            LogName[0],
                            StringComparison.OrdinalIgnoreCase))
                        .ToArray();
            case "GenericEvents":
                usesFiles = false;
                return ExpandCheckpointChannels(
                    NormalizeRequiredValues(
                        LogName,
                        nameof(LogName)));
            case "ProviderEvents":
                usesFiles = false;
                return ResolveCheckpointProviderChannels(
                    ProviderName ?? Array.Empty<string>());
            default:
                usesFiles = false;
                return Array.Empty<string>();
        }
    }

    private int GetCheckpointSourceCount() {
        return GetCheckpointSources(out _).Count;
    }

    private IReadOnlyList<string> ResolveCheckpointProviderChannels(
        IEnumerable<string> providerPatterns) {

        var channels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string? machine in
                 EventLogTarget.NormalizeMachineNames(MachineName)) {
            var catalogQuery = new EventLogCatalogQuery {
                MachineName = machine,
                Credential = Credential?.GetNetworkCredential(),
                Authentication = Authentication,
                ConnectionTimeoutMilliseconds =
                    EffectiveRemoteConnectionTimeoutMilliseconds,
                Culture = MessageCulture
            };
            foreach (string channel in
                     EventLogCatalog.ResolveProviderChannels(
                         catalogQuery,
                         providerPatterns,
                         CancelToken)) {
                channels.Add(channel);
            }
        }
        return channels
            .OrderBy(static channel => channel, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<string> ExpandCheckpointChannels(
        IReadOnlyList<string> logNames) {

        var channels = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        foreach (string? machine in
                 EventLogTarget.NormalizeMachineNames(MachineName)) {
            foreach (string channel in
                     ExpandChannelPatterns(logNames, machine)) {
                channels.Add(channel);
            }
        }
        return channels
            .OrderBy(static channel => channel, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

}

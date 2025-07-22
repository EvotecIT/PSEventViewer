using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;

namespace EventViewerX {
    /// <summary>
    /// Functions for retrieving event provider metadata.
    /// </summary>
    public partial class SearchEvents : Settings {

        /// <summary>
        /// Cache of provider metadata keyed by provider name to avoid repeated lookups.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Metadata> _providerMetadataCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Retrieves available event providers on the local machine.
        /// </summary>
        /// <returns>Enumeration of provider metadata.</returns>
        public static IEnumerable<Metadata> GetProviders() {
            using EventLogSession session = new();
            foreach (string providerName in session.GetProviderNames()) {
                if (!_providerMetadataCache.TryGetValue(providerName, out var metadata)) {
                    try {
                        using ProviderMetadata providerMetadata = new(providerName, session, CultureInfo.CurrentCulture);
                        metadata = new Metadata(providerName, providerMetadata);
                        _providerMetadataCache[providerName] = metadata;
                    } catch (EventLogInvalidDataException ex) {
                        _logger.WriteWarning($"Error reading data for provider {providerName}: {ex.Message}");
                    } catch (EventLogException ex) {
                        _logger.WriteWarning($"Error reading metadata for provider {providerName}: {ex.Message}");
                    } catch (UnauthorizedAccessException ex) {
                        _logger.WriteWarning($"Access denied reading metadata for provider {providerName}: {ex.Message}");
                    } catch (Exception ex) {
                        _logger.WriteWarning($"Error reading metadata for provider {providerName}: {ex.Message}");
                    }
                }

                if (metadata != null) {
                    yield return metadata;
                }
            }
        }

        /// <summary>
        /// Returns provider metadata as a list.
        /// </summary>
        public static List<Metadata> GetProviderList() {
            return GetProviders().ToList();
        }

    }

    /// <summary>
    /// Lightweight provider metadata container.
    /// </summary>
    public class Metadata {
        public string ProviderName;
        public string DisplayName;
        public List<string> LogNames;
        public Guid Id;

        public IList<EventLogLink> LogLinks { get; set; }
        public IEnumerable<EventMetadata> Events { get; set; }

        public IList<EventKeyword> Keywords { get; set; }

        public IList<EventOpcode> Opcodes { get; set; }

        public string MessageFilePath { get; set; }

        public string ResourceFilePath { get; set; }

        public string ParameterFilePath { get; set; }

        public IList<EventTask> Tasks { get; set; }

        public List<string> Errors = new List<string>();

        public Metadata(string providerName, ProviderMetadata providerMetadata) {
            ProviderName = providerName;
            Id = providerMetadata.Id;
            MessageFilePath = providerMetadata.MessageFilePath;
            Keywords = providerMetadata.Keywords?.ToList();

            TrySetMetadata(() => DisplayName = providerMetadata.DisplayName, "display name", providerName);
            TrySetMetadata(() => LogLinks = providerMetadata.LogLinks?.ToList(), "log links", providerName);
            TrySetMetadata(() => LogNames = providerMetadata.LogLinks?.Select(link => link.LogName).ToList() ?? new List<string>(), "log names", providerName);
            TrySetMetadata(() => ParameterFilePath = providerMetadata.ParameterFilePath, "parameter file path", providerName);
            TrySetMetadata(() => ResourceFilePath = providerMetadata.ResourceFilePath, "resource file path", providerName);
            TrySetMetadata(() => Opcodes = providerMetadata.Opcodes?.ToList(), "opcodes", providerName);
            TrySetMetadata(() => Tasks = providerMetadata.Tasks?.ToList(), "tasks", providerName);
            TrySetMetadata(() => Events = providerMetadata.Events?.ToList(), "events", providerName);
        }

        private void TrySetMetadata(Action setAction, string metadataType, string providerName) {
            try {
                setAction();
            } catch (Exception ex) {
                Errors.Add($"Error reading {metadataType} for provider {providerName}. Details: {ex.Message}");
            }
        }
    }
}
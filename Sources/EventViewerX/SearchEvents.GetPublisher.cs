using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;

namespace EventViewerX {
    public partial class SearchEvents : Settings {

        public static IEnumerable<Metadata> GetProviders() {
            EventLogSession session = new EventLogSession();
            foreach (string providerName in session.GetProviderNames()) {
                Metadata metadata = null;
                try {
                    using ProviderMetadata providerMetadata = new ProviderMetadata(providerName);
                    metadata = new Metadata(providerName, providerMetadata);
                } catch (EventLogInvalidDataException ex) {
                    _logger.WriteWarning($"Error reading data for provider {providerName}: {ex.Message}");
                } catch (EventLogException ex) {
                    _logger.WriteWarning($"Error reading metadata for provider {providerName}: {ex.Message}");
                } catch (UnauthorizedAccessException ex) {
                    _logger.WriteWarning($"Access denied reading metadata for provider {providerName}: {ex.Message}");
                } catch (Exception ex) {
                    _logger.WriteWarning($"Error reading metadata for provider {providerName}: {ex.Message}");
                }
                if (metadata != null) {
                    yield return metadata;
                }
            }
        }

        public static List<Metadata> GetProviderList() {
            return GetProviders().ToList();
        }

    }

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
            Keywords = providerMetadata.Keywords;

            TrySetMetadata(() => DisplayName = providerMetadata.DisplayName, "display name", providerName);
            TrySetMetadata(() => LogLinks = providerMetadata.LogLinks, "log links", providerName);
            TrySetMetadata(() => LogNames = providerMetadata.LogLinks.Select(link => link.LogName).ToList(), "log names", providerName);
            TrySetMetadata(() => ParameterFilePath = providerMetadata.ParameterFilePath, "parameter file path", providerName);
            TrySetMetadata(() => ResourceFilePath = providerMetadata.ResourceFilePath, "resource file path", providerName);
            TrySetMetadata(() => Opcodes = providerMetadata.Opcodes, "opcodes", providerName);
            TrySetMetadata(() => Tasks = providerMetadata.Tasks, "tasks", providerName);
            TrySetMetadata(() => Events = providerMetadata.Events, "events", providerName);
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

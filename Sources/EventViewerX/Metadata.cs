using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;

namespace EventViewerX {
    /// <summary>
    /// Lightweight provider metadata container.
    /// </summary>
    public class Metadata {
        public string ProviderName { get; }
        public string? DisplayName { get; private set; }
        public List<string> LogNames { get; private set; } = new();
        public Guid Id { get; }

        public IList<EventLogLink>? LogLinks { get; private set; }
        public IEnumerable<EventMetadata>? Events { get; private set; }

        public IList<EventKeyword>? Keywords { get; private set; }

        public IList<EventOpcode>? Opcodes { get; private set; }

        public string MessageFilePath { get; private set; } = string.Empty;

        public string ResourceFilePath { get; private set; } = string.Empty;

        public string ParameterFilePath { get; private set; } = string.Empty;

        public IList<EventTask>? Tasks { get; private set; }

        public List<string> Errors { get; } = new();

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


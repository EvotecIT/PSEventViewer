using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;

namespace EventViewerX {
    /// <summary>
    /// Lightweight provider metadata container.
    /// </summary>
    public class Metadata {
        /// <summary>Provider name as returned by the event log service.</summary>
        public string ProviderName { get; }
        /// <summary>Localized display name when available from provider resources.</summary>
        public string? DisplayName { get; private set; }
        /// <summary>Logs that the provider writes to.</summary>
        public List<string> LogNames { get; private set; } = new();
        /// <summary>Unique provider identifier (GUID).</summary>
        public Guid Id { get; }

        /// <summary>Links from provider metadata to the log definitions.</summary>
        public IList<EventLogLink>? LogLinks { get; private set; }
        /// <summary>Event metadata entries exposed by the provider.</summary>
        public IEnumerable<EventMetadata>? Events { get; private set; }

        /// <summary>Keywords defined by the provider for filtering.</summary>
        public IList<EventKeyword>? Keywords { get; private set; }

        /// <summary>Opcodes defined by the provider for filtering.</summary>
        public IList<EventOpcode>? Opcodes { get; private set; }

        /// <summary>Path to the message DLL used for formatting descriptions.</summary>
        public string MessageFilePath { get; private set; } = string.Empty;

        /// <summary>Path to the resource DLL referenced by the provider.</summary>
        public string ResourceFilePath { get; private set; } = string.Empty;

        /// <summary>Path to the parameter DLL referenced by the provider.</summary>
        public string ParameterFilePath { get; private set; } = string.Empty;

        /// <summary>Tasks defined by the provider.</summary>
        public IList<EventTask>? Tasks { get; private set; }

        /// <summary>Non-fatal metadata reading errors captured during initialization.</summary>
        public List<string> Errors { get; } = new();

        /// <summary>
        /// Builds a metadata snapshot from <see cref="ProviderMetadata"/> returned by the event log API.
        /// </summary>
        /// <param name="providerName">Name of the provider being queried.</param>
        /// <param name="providerMetadata">Raw provider metadata from the event log API.</param>
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

        /// <summary>
        /// Minimal constructor used when full <see cref="ProviderMetadata"/> cannot be read.
        /// Creates an object with just the provider name and empty collections to keep APIs stable.
        /// </summary>
        /// <param name="providerName">Name of the provider that could not be fully loaded.</param>
        public Metadata(string providerName) {
            ProviderName = providerName;
            Id = Guid.Empty;
            Events = Array.Empty<EventMetadata>();
            LogNames = new List<string>();
            Errors.Add("ProviderMetadata unavailable in current environment.");
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


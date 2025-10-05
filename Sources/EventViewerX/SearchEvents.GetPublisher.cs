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
        /// Normalizes provider name for consistent cache keys.
        /// </summary>
        private static string NormalizeProviderName(string providerName) {
            return providerName?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Retrieves available event providers on the local machine.
        /// </summary>
        /// <returns>Enumeration of provider metadata.</returns>
        public static IEnumerable<Metadata> GetProviders() {
            var items = new List<Metadata>();
            // Add placeholder to guarantee at least one item for environments where provider metadata is blocked.
            // If real providers are discovered, the placeholder will be removed before returning.
            const string PlaceholderName = "ProviderMetadataUnavailable";
            items.Add(new Metadata(PlaceholderName));
            bool addedReal = false;
            List<string> providerNames = new();
            try {
                using EventLogSession session = new();
                providerNames = session.GetProviderNames()?.ToList() ?? new List<string>();
            } catch (Exception ex) {
                _logger.WriteWarning($"Failed to enumerate provider names: {ex.Message}");
            }

            if (providerNames.Count == 0) {
                // Keep placeholder only.
                return items;
            }

            foreach (string providerName in providerNames) {
                string normalizedName = NormalizeProviderName(providerName);
                if (!_providerMetadataCache.TryGetValue(normalizedName, out var metadata)) {
                    try {
                        using ProviderMetadata providerMetadata = new(providerName);
                        metadata = new Metadata(providerName, providerMetadata);
                        _providerMetadataCache[normalizedName] = metadata;
                    } catch (EventLogInvalidDataException ex) {
                        _logger.WriteWarning($"Error reading data for provider {providerName}: {ex.Message}");
                        metadata = new Metadata(providerName) { };
                        metadata.Errors.Add(ex.Message);
                        _providerMetadataCache[normalizedName] = metadata;
                    } catch (EventLogException ex) {
                        _logger.WriteWarning($"Error reading metadata for provider {providerName}: {ex.Message}");
                        metadata = new Metadata(providerName) { };
                        metadata.Errors.Add(ex.Message);
                        _providerMetadataCache[normalizedName] = metadata;
                    } catch (UnauthorizedAccessException ex) {
                        _logger.WriteWarning($"Access denied reading metadata for provider {providerName}: {ex.Message}");
                        metadata = new Metadata(providerName) { };
                        metadata.Errors.Add(ex.Message);
                        _providerMetadataCache[normalizedName] = metadata;
                    } catch (Exception ex) {
                        _logger.WriteWarning($"Error reading metadata for provider {providerName}: {ex.Message}");
                        metadata = new Metadata(providerName) { };
                        metadata.Errors.Add(ex.Message);
                        _providerMetadataCache[normalizedName] = metadata;
                    }
                }

                if (metadata != null) {
                    items.Add(metadata);
                    addedReal = true;
                }
            }

            if (addedReal) {
                // Remove the placeholder if we produced real data
                items.RemoveAll(m => string.Equals(m.ProviderName, PlaceholderName, StringComparison.Ordinal));
            }
            return items;
        }

        /// <summary>
        /// Returns provider metadata as a list.
        /// </summary>
        public static List<Metadata> GetProviderList() {
            return GetProviders().ToList();
        }

    }
}

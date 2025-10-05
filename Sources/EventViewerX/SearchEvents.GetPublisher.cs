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
            using EventLogSession session = new();
            foreach (string providerName in session.GetProviderNames()) {
                string normalizedName = NormalizeProviderName(providerName);
                if (!_providerMetadataCache.TryGetValue(normalizedName, out var metadata)) {
                    try {
                        using ProviderMetadata providerMetadata = new(providerName, session, CultureInfo.CurrentCulture);
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
}

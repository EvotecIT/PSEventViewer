using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace EventViewerX {
    public partial class SearchEvents {
        /// <summary>
        /// Retrieves execution records (event 4100) for PowerShell script blocks.
        /// </summary>
        /// <param name="type">Windows PowerShell or PowerShell Core.</param>
        /// <param name="machineName">Remote machine to query; <c>null</c> targets local logs.</param>
        /// <param name="eventLogPath">Optional .evtx file path to read from instead of the live log.</param>
        /// <param name="dateFrom">Lower bound for the query time window.</param>
        /// <param name="dateTo">Upper bound for the query time window.</param>
        /// <param name="maxEvents">Maximum records to return. Zero returns every matching record.</param>
        /// <param name="maxEventsScanned">Maximum native event records to scan. Zero scans the complete query.</param>
        /// <param name="cancellationToken">Cancellation token used to interrupt native reads.</param>
        /// <param name="executionInfo">Optional reusable completion record populated while the query runs.</param>
        /// <returns>Execution records in reverse chronological order.</returns>
        public static IEnumerable<PowerShellScriptExecutionInfo> GetPowerShellScriptExecution(
            PowerShellEdition type,
            string? machineName = null,
            string? eventLogPath = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            int maxEvents = 0,
            int maxEventsScanned = 0,
            CancellationToken cancellationToken = default,
            PowerShellScriptQueryExecutionInfo? executionInfo = null) {

            ValidatePowerShellScriptLimits(maxEvents, nameof(maxEvents), maxEventsScanned);
            cancellationToken.ThrowIfCancellationRequested();
            PowerShellScriptQueryExecutionInfo queryInfo = executionInfo ?? new PowerShellScriptQueryExecutionInfo();
            queryInfo.Reset(machineName, eventLogPath, maxEvents, maxEventsScanned);
            string logName = type == PowerShellEdition.WindowsPowerShell
                ? "Microsoft-Windows-PowerShell/Operational"
                : "PowerShellCore/Operational";

            string queryString = BuildWinEventFilter(
                id: new[] { "4100" },
                startTime: dateFrom,
                endTime: dateTo,
                logName: logName,
                path: eventLogPath,
                xpathOnly: false);

            EventLogSession? session = null;
            EventLogQuery query = string.IsNullOrEmpty(eventLogPath)
                ? new EventLogQuery(logName, PathType.LogName, queryString)
                : new EventLogQuery(null, PathType.LogName, queryString);
            query.ReverseDirection = true;
            if (!string.IsNullOrEmpty(machineName)) {
                EventLogSessionOpenResult sessionResult = CreateSessionResult(machineName, "PowerShellScripts", logName, DefaultSessionTimeoutMs);
                session = sessionResult.Session;
                if (session == null) {
                    try {
                        ThrowSessionFailure(sessionResult);
                    } finally {
                        sessionResult.Dispose();
                    }
                }
                query.Session = session;
            }

            try {
                using EventLogReader reader = CreateEventLogReader(query, machineName, DefaultSessionTimeoutMs);
                using CancellationTokenRegistration readerCancellation = RegisterReaderCancellation(reader, cancellationToken);
                int scanned = 0;
                int returned = 0;

                // Reverse direction keeps newest script blocks first; each native read owns the timeout directly.
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (maxEventsScanned > 0 && scanned >= maxEventsScanned) {
                        queryInfo.ScanLimitReached = true;
                        yield break;
                    }

                    EventRecord? record;
                    try {
                        record = ReadEventWithCancellation(reader, DefaultSessionTimeoutMs, $"Reading PowerShell script events from '{logName}'", cancellationToken);
                    } catch (EventLogException ex) {
                        _logger.WriteWarning($"PowerShellScripts: error reading log on {machineName ?? GetFQDN()}: {ex.Message}");
                        throw;
                    }

                    if (record == null) break;
                    scanned++;
                    queryInfo.EventsScanned = scanned;

                    var eventObject = new EventObject(record, machineName ?? eventLogPath ?? GetFQDN(), EventReadMode.StructuredData);
                    var element = XElement.Parse(eventObject.XMLData);
                    string? contextInfo = ExtractData(element, "ContextInfo");
                    var data = ParseContextInfo(contextInfo);
                    returned++;
                    queryInfo.ResultsReturned = returned;
                    bool outputLimitReached = maxEvents > 0 && returned >= maxEvents;
                    if (outputLimitReached) {
                        queryInfo.OutputLimitReached = true;
                    }
                    yield return new PowerShellScriptExecutionInfo(eventObject, data);
                    if (outputLimitReached) {
                        yield break;
                    }
                }
            }
            finally {
                session?.Dispose();
            }
        }

        /// <summary>
        /// Reassembles PowerShell script blocks (events 4103/4104) from the operational log.
        /// </summary>
        /// <param name="type">Windows PowerShell or PowerShell Core.</param>
        /// <param name="machineName">Remote machine to query; <c>null</c> targets local logs.</param>
        /// <param name="eventLogPath">Optional .evtx file path to read from instead of the live log.</param>
        /// <param name="dateFrom">Lower bound for the query time window.</param>
        /// <param name="dateTo">Upper bound for the query time window.</param>
        /// <param name="format">When true, re-indents the captured script text.</param>
        /// <param name="containsText">Optional text filters applied to reconstructed scripts.</param>
        /// <param name="maxScripts">Maximum reconstructed scripts to return. Zero returns every matching script.</param>
        /// <param name="maxEventsScanned">Maximum native records to scan. Zero scans the complete query.</param>
        /// <param name="maxPendingScripts">Maximum incomplete script groups retained while scanning.</param>
        /// <param name="maxCachedEvents">Maximum event snapshots retained across incomplete script groups.</param>
        /// <param name="cancellationToken">Cancellation token used to interrupt native reads.</param>
        /// <param name="executionInfo">Optional reusable completion record populated while the query runs.</param>
        /// <returns>Restored script blocks in reverse chronological order.</returns>
        public static IEnumerable<RestoredPowerShellScript> RestorePowerShellScripts(
            PowerShellEdition type,
            string? machineName = null,
            string? eventLogPath = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            bool format = false,
            IEnumerable<string>? containsText = null,
            int maxScripts = 0,
            int maxEventsScanned = 0,
            int maxPendingScripts = DefaultPowerShellScriptPendingLimit,
            int maxCachedEvents = DefaultPowerShellScriptEventCacheLimit,
            CancellationToken cancellationToken = default,
            PowerShellScriptQueryExecutionInfo? executionInfo = null) {

            ValidatePowerShellScriptLimits(maxScripts, nameof(maxScripts), maxEventsScanned);
            cancellationToken.ThrowIfCancellationRequested();
            if (maxPendingScripts <= 0) {
                throw new ArgumentOutOfRangeException(nameof(maxPendingScripts), "Maximum pending scripts must be positive.");
            }
            if (maxCachedEvents <= 0) {
                throw new ArgumentOutOfRangeException(nameof(maxCachedEvents), "Maximum cached events must be positive.");
            }
            bool ownsQueryInfo = executionInfo == null;
            PowerShellScriptQueryExecutionInfo queryInfo = executionInfo ?? new PowerShellScriptQueryExecutionInfo();
            queryInfo.Reset(machineName, eventLogPath, maxScripts, maxEventsScanned, maxPendingScripts, maxCachedEvents);

            string[] textFilters = containsText?
                .Where(static term => term != null)
                .ToArray() ?? Array.Empty<string>();
            string logName = type == PowerShellEdition.WindowsPowerShell
                ? "Microsoft-Windows-PowerShell/Operational"
                : "PowerShellCore/Operational";

            string queryString = BuildWinEventFilter(
                id: new[] { "4103", "4104" },
                startTime: dateFrom,
                endTime: dateTo,
                logName: logName,
                path: eventLogPath,
                xpathOnly: false);

            EventLogSession? session = null;
            EventLogQuery query = string.IsNullOrEmpty(eventLogPath)
                ? new EventLogQuery(logName, PathType.LogName, queryString)
                : new EventLogQuery(null, PathType.LogName, queryString);
            query.ReverseDirection = true;
            if (!string.IsNullOrEmpty(machineName)) {
                EventLogSessionOpenResult sessionResult = CreateSessionResult(machineName, "PowerShellScripts", logName, DefaultSessionTimeoutMs);
                session = sessionResult.Session;
                if (session == null) {
                    try {
                        ThrowSessionFailure(sessionResult);
                    } finally {
                        sessionResult.Dispose();
                    }
                }
                query.Session = session;
            }

            var cache = new PowerShellScriptFragmentCache(maxPendingScripts, maxCachedEvents);
            try {
                using EventLogReader reader = CreateEventLogReader(query, machineName, DefaultSessionTimeoutMs);
                using CancellationTokenRegistration readerCancellation = RegisterReaderCancellation(reader, cancellationToken);
                int scanned = 0;
                int returned = 0;
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (maxEventsScanned > 0 && scanned >= maxEventsScanned) {
                        queryInfo.ScanLimitReached = true;
                        _logger.WriteVerbose($"PowerShellScripts: stopped after reaching the {maxEventsScanned} event scan limit.");
                        break;
                    }

                    EventRecord? record;
                    try {
                        record = ReadEventWithCancellation(reader, DefaultSessionTimeoutMs, $"Reading PowerShell script events from '{logName}'", cancellationToken);
                    } catch (EventLogException ex) {
                        _logger.WriteWarning($"PowerShellScripts: error reading log on {machineName ?? GetFQDN()}: {ex.Message}");
                        throw;
                    }

                    if (record == null) break;
                    scanned++;
                    queryInfo.EventsScanned = scanned;

                    var eventObject = new EventObject(record, machineName ?? eventLogPath ?? GetFQDN(), EventReadMode.StructuredData);
                    var element = XElement.Parse(eventObject.XMLData);
                    string? scriptText = ExtractData(element, "ScriptBlockText");
                    if (string.IsNullOrEmpty(scriptText) || scriptText == "0") {
                        continue;
                    }
                    string nonNullScriptText = scriptText!;
                    string? scriptId = ExtractData(element, "ScriptBlockId");
                    if (scriptId == null) {
                        continue;
                    }
                    int messageNumber = ParseBoundedFragmentNumber(ExtractData(element, "MessageNumber"), out bool invalidMessageNumber);
                    int messageTotal = ParseBoundedFragmentNumber(ExtractData(element, "MessageTotal"), out bool invalidMessageTotal);
                    if (invalidMessageNumber || invalidMessageTotal) {
                        queryInfo.InvalidFragmentMetadataEvents++;
                    }
                    if (invalidMessageNumber) {
                        continue;
                    }
                    if (cache.TryAdd(scriptId, messageNumber, messageTotal, nonNullScriptText, eventObject, out PowerShellScriptAssembly? completed) &&
                        completed != null &&
                        TryBuildRestoredPowerShellScript(completed, format, textFilters, out RestoredPowerShellScript restored)) {
                        if (!restored.IsComplete) {
                            queryInfo.IncompleteScriptsReturned++;
                        }
                        returned++;
                        queryInfo.ResultsReturned = returned;
                        bool outputLimitReached = maxScripts > 0 && returned >= maxScripts;
                        if (outputLimitReached) {
                            queryInfo.OutputLimitReached = true;
                        }
                        yield return restored;
                        if (outputLimitReached) {
                            _logger.WriteVerbose($"PowerShellScripts: stopped after returning the configured maximum of {maxScripts} scripts.");
                            yield break;
                        }
                    }
                }

                foreach (PowerShellScriptAssembly pending in cache.Drain()) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!TryBuildRestoredPowerShellScript(pending, format, textFilters, out RestoredPowerShellScript restored)) {
                        continue;
                    }

                    if (!restored.IsComplete) {
                        queryInfo.IncompleteScriptsReturned++;
                    }
                    returned++;
                    queryInfo.ResultsReturned = returned;
                    bool outputLimitReached = maxScripts > 0 && returned >= maxScripts;
                    if (outputLimitReached) {
                        queryInfo.OutputLimitReached = true;
                    }
                    yield return restored;
                    if (outputLimitReached) {
                        _logger.WriteVerbose($"PowerShellScripts: stopped after returning the configured maximum of {maxScripts} scripts.");
                        yield break;
                    }
                }
            }
            finally {
                queryInfo.EvictedIncompleteScripts = cache.EvictedScriptCount;
                queryInfo.EvictedCachedEvents = cache.EvictedEventCount;
                if (ownsQueryInfo && cache.EvictedScriptCount > 0) {
                    _logger.WriteWarning(
                        $"PowerShellScripts: evicted {cache.EvictedScriptCount} incomplete script groups " +
                        $"containing {cache.EvictedEventCount} events after reaching the configured cache bounds.");
                }
                session?.Dispose();
            }
        }

        private static void ValidatePowerShellScriptLimits(int maxOutput, string maxOutputParameterName, int maxEventsScanned) {
            if (maxOutput < 0) {
                throw new ArgumentOutOfRangeException(maxOutputParameterName, "Maximum output events must be greater than or equal to zero.");
            }
            if (maxEventsScanned < 0) {
                throw new ArgumentOutOfRangeException(nameof(maxEventsScanned), "Maximum scanned events must be greater than or equal to zero.");
            }
        }

        private static int ParseBoundedFragmentNumber(string? value, out bool invalid) {
            invalid = false;
            if (string.IsNullOrWhiteSpace(value)) {
                return 0;
            }

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ||
                parsed < 0 ||
                parsed > MaximumPowerShellScriptPartCount) {
                invalid = true;
                return 0;
            }

            return parsed;
        }

        private static bool TryBuildRestoredPowerShellScript(
            PowerShellScriptAssembly assembly,
            bool format,
            IReadOnlyList<string> containsText,
            out RestoredPowerShellScript restored) {

            var scriptBuilder = new StringBuilder();
            foreach (KeyValuePair<int, string> part in assembly.Parts.OrderBy(static pair => pair.Key)) {
                scriptBuilder.Append(part.Value);
            }

            string script = scriptBuilder.ToString();
            for (int index = 0; index < containsText.Count; index++) {
                if (script.IndexOf(containsText[index], StringComparison.OrdinalIgnoreCase) < 0) {
                    restored = null!;
                    return false;
                }
            }

            if (format) {
                script = FormatScript(script);
            }

            EventObject metaRecord = assembly.MetaRecord ?? assembly.Events[0];
            var metaElement = XElement.Parse(metaRecord.XMLData);
            restored = new RestoredPowerShellScript {
                ScriptBlockId = assembly.ScriptBlockId,
                Script = script,
                IsComplete = assembly.IsComplete,
                ExpectedPartCount = assembly.ExpectedParts,
                AvailablePartCount = assembly.Parts.Count,
                Events = assembly.Events,
                Data = GetAllData(metaElement)
            };
            return true;
        }
    }
}

using System;

namespace EventViewerX {
    public partial class EventObject {
        /// <summary>
        /// Attempts to return a message line by index. Negative indices address from the end (for example, <c>-1</c> is the last line).
        /// </summary>
        /// <param name="index">Zero-based line index, or negative index from the end.</param>
        /// <param name="line">Resolved line value when found; otherwise empty string.</param>
        /// <param name="trim">When <c>true</c>, trims whitespace from the returned line.</param>
        /// <returns><c>true</c> when the line exists; otherwise <c>false</c>.</returns>
        public bool TryGetMessageLine(int index, out string line, bool trim = true) {
            line = string.Empty;
            IReadOnlyList<string>? messageLines = MessageLines;
            if (messageLines == null || messageLines.Count == 0) {
                return false;
            }

            int effectiveIndex = index < 0 ? messageLines.Count + index : index;
            if (effectiveIndex < 0 || effectiveIndex >= messageLines.Count) {
                return false;
            }

            string value = messageLines[effectiveIndex] ?? string.Empty;
            line = trim ? value.Trim() : value;
            return true;
        }

        /// <summary>
        /// Returns a message line by index or an empty string when missing. Negative indices address from the end.
        /// </summary>
        /// <param name="index">Zero-based line index, or negative index from the end.</param>
        /// <param name="trim">When <c>true</c>, trims whitespace from the returned line.</param>
        /// <returns>The requested message line or empty string when out of range.</returns>
        public string GetMessageLineOrEmpty(int index, bool trim = true) {
            return TryGetMessageLine(index, out string line, trim) ? line : string.Empty;
        }

        /// <summary>
        /// Gets the value from data dictionary.
        /// </summary>
        /// <param name="key1">The key1.</param>
        /// <param name="key2">The key2.</param>
        /// <param name="splitter">The splitter.</param>
        /// <param name="reverseOrder">if set to <c>true</c> [reverse order].</param>
        /// <returns></returns>
        public string GetValueFromDataDictionary(string key1, string? key2 = null, string splitter = "\\", bool reverseOrder = false) {
            if (key1 != null && key2 != null && Data.ContainsKey(key1) && Data.ContainsKey(key2)) {
                if (reverseOrder) {
                    return Data[key2] + splitter + Data[key1];
                } else {
                    return Data[key1] + splitter + Data[key2];
                }
            } else if (key1 != null && Data.ContainsKey(key1)) {
                return Data[key1];
            } else if (key2 != null && Data.ContainsKey(key2)) {
                return Data[key2];
            } else {
                return "";
            }
        }

        internal bool ValueMatches(string key, string expectedValue) {
            if (key != null && Data.ContainsKey(key)) {
                if (Data[key].Equals(expectedValue, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                } else {
                    return false;
                }
            }

            return false;
        }
    }
}


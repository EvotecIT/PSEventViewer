using System;

namespace EventViewerX {
    public partial class EventObject {
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


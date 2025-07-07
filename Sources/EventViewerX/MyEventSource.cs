using System.Diagnostics.Tracing;

namespace EventViewerX {

    /// <summary>
    /// EventSource used for internal diagnostic events.
    /// </summary>
    [EventSource(Name = "MyCompany-MyApplication-MyEventSource")]
    public sealed class MyEventSource : EventSource {
        /// <summary>
        /// Singleton instance for writing events.
        /// </summary>
        public static readonly MyEventSource Log = new MyEventSource();

        /// <summary>
        /// Writes a custom informational event.
        /// </summary>
        /// <param name="eventId">Identifier of the event.</param>
        /// <param name="param1">First event parameter.</param>
        /// <param name="param2">Second event parameter.</param>
        /// <param name="binaryData">Binary data payload.</param>
        public void MyEvent(int eventId, string param1, string param2, string binaryData) {
            if (eventId > 0) {
                WriteEvent(eventId, param1, param2, binaryData);
            }
        }
    }

    /// <summary>
    /// Helper class simplifying writing to the custom event source.
    /// </summary>
    public class EventLogger {
        /// <summary>
        /// Writes an event to the configured event source.
        /// </summary>
        /// <param name="eventSourceName">Name of the event source.</param>
        /// <param name="eventId">Event identifier.</param>
        /// <param name="level">Event level.</param>
        /// <param name="param1">First parameter.</param>
        /// <param name="param2">Second parameter.</param>
        /// <param name="binaryData">Optional binary payload.</param>
        public static void WriteToEventLog(string eventSourceName, int eventId, EventLevel level, string param1, string param2, string binaryData) {
            MyEventSource.Log.MyEvent(eventId, param1, param2, binaryData);
        }
    }
}
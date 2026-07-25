using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace EventViewerX.Native;

internal static class WindowsEventReader {
    internal static IEnumerable<EventObject> Read(
        NativeEventQuery query,
        EventReadMode readMode,
        string queriedMachine,
        string containerLog,
        CancellationToken cancellationToken) {

        switch (readMode) {
            case EventReadMode.Metadata:
                foreach (EventObject eventObject in ReadMetadataEvents(
                             query,
                             queriedMachine,
                             containerLog,
                             cancellationToken)) {
                    yield return eventObject;
                }
                break;
            case EventReadMode.Message:
                foreach (NativeEventMessage message in ReadMessages(query, cancellationToken)) {
                    yield return new EventObject(message, queriedMachine, containerLog);
                }
                break;
            case EventReadMode.StructuredData:
                foreach (NativeEventStructured structured in ReadStructured(query, cancellationToken)) {
                    yield return new EventObject(structured, queriedMachine, containerLog);
                }
                break;
            case EventReadMode.RawXml:
                foreach (EventObject eventObject in ReadRawXml(
                             query,
                             queriedMachine,
                             containerLog,
                             cancellationToken)) {
                    yield return eventObject;
                }
                break;
            case EventReadMode.Full:
                foreach (NativeEventFull full in ReadFull(query, cancellationToken)) {
                    yield return new EventObject(full, queriedMachine, containerLog);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(readMode), readMode, "Unsupported event read mode.");
        }
    }

    internal static IEnumerable<NativeEventMetadata> ReadMetadata(
        NativeEventQuery query,
        CancellationToken cancellationToken) {

        return ReadMetadataIterator(query, cancellationToken);
    }

    private static IEnumerable<NativeEventMetadata> ReadMetadataIterator(
        NativeEventQuery query,
        CancellationToken cancellationToken) {

        using var renderer = new WindowsEventSystemRenderer();
        foreach (NativeEventMetadata metadata in ReadEvents(
                     query,
                     cancellationToken,
                     renderer.Render)) {
            yield return metadata;
        }
    }

    private static IEnumerable<EventObject> ReadMetadataEvents(
        NativeEventQuery query,
        string queriedMachine,
        string containerLog,
        CancellationToken cancellationToken) {

        using var systemRenderer = new WindowsEventSystemRenderer();
        using var bookmarkRenderer = query.IncludeBookmark
            ? new WindowsEventBookmarkRenderer()
            : null;
        foreach (EventObject eventObject in ReadEvents(
                     query,
                     cancellationToken,
                     eventHandle => new EventObject(
                         systemRenderer.Render(eventHandle),
                         bookmarkRenderer?.Render(eventHandle),
                         queriedMachine,
                         containerLog))) {
            yield return eventObject;
        }
    }

    internal static IEnumerable<NativeEventMessage> ReadMessages(
        NativeEventQuery query,
        CancellationToken cancellationToken) {

        return ReadMessagesIterator(query, cancellationToken);
    }

    internal static IEnumerable<NativeEventStructured> ReadStructured(
        NativeEventQuery query,
        CancellationToken cancellationToken) {

        return ReadStructuredIterator(query, cancellationToken);
    }

    internal static IEnumerable<NativeEventFull> ReadFull(
        NativeEventQuery query,
        CancellationToken cancellationToken) {

        return ReadFullIterator(query, cancellationToken);
    }

    internal static IEnumerable<string> ReadXml(
        NativeEventQuery query,
        CancellationToken cancellationToken) {

        return ReadXmlIterator(query, cancellationToken);
    }

    private static IEnumerable<EventObject> ReadRawXml(
        NativeEventQuery query,
        string queriedMachine,
        string containerLog,
        CancellationToken cancellationToken) {

        using var systemRenderer = new WindowsEventSystemRenderer();
        using var xmlRenderer = new WindowsEventXmlRenderer();
        using var bookmarkRenderer = query.IncludeBookmark
            ? new WindowsEventBookmarkRenderer()
            : null;
        foreach (EventObject eventObject in ReadEvents(
                     query,
                     cancellationToken,
                     eventHandle => new EventObject(
                         systemRenderer.Render(eventHandle),
                         xmlRenderer.Render(eventHandle),
                         bookmarkRenderer?.Render(eventHandle),
                         queriedMachine,
                         containerLog))) {
            yield return eventObject;
        }
    }

    internal static long CopyXml(
        NativeEventQuery query,
        Stream destination,
        long maxEvents,
        CancellationToken cancellationToken) {

        if (destination == null) {
            throw new ArgumentNullException(nameof(destination));
        }

        long count = 0;
        using var renderer = new WindowsEventXmlRenderer();
        using var events = new WindowsEventHandleEnumerator(query, cancellationToken);
        while ((maxEvents == 0 || count < maxEvents) && events.MoveNext()) {
            renderer.Write(events.Current, destination);
            count++;
        }
        return count;
    }

    private static IEnumerable<string> ReadXmlIterator(
        NativeEventQuery query,
        CancellationToken cancellationToken) {

        using var renderer = new WindowsEventXmlRenderer();
        foreach (string xml in ReadEvents(
                     query,
                     cancellationToken,
                     renderer.Render)) {
            yield return xml;
        }
    }

    private static IEnumerable<NativeEventMessage> ReadMessagesIterator(
        NativeEventQuery query,
        CancellationToken cancellationToken) {

        using var systemRenderer = new WindowsEventSystemRenderer();
        using var messageRenderer = new WindowsEventMessageRenderer(
            query.Session,
            query.PublisherMetadataPath,
            query.MessageLocale,
            query.FallbackMessageLocale);
        foreach (NativeEventMessage message in ReadEvents(
                     query,
                     cancellationToken,
                     eventHandle => {
                         NativeEventMetadata metadata =
                             systemRenderer.Render(eventHandle);
                         return messageRenderer.Render(
                             eventHandle,
                             metadata,
                             query.IncludeBookmark);
                     })) {
            yield return message;
        }
    }

    private static IEnumerable<NativeEventStructured> ReadStructuredIterator(
        NativeEventQuery query,
        CancellationToken cancellationToken) {

        using var systemRenderer = new WindowsEventSystemRenderer();
        using var payloadRenderer = new WindowsEventPayloadRenderer();
        foreach (NativeEventStructured structured in ReadEvents(
                     query,
                     cancellationToken,
                     eventHandle => {
                         NativeEventMetadata metadata =
                             systemRenderer.Render(eventHandle);
                         return payloadRenderer.Render(
                             eventHandle,
                             metadata,
                             query.IncludeBookmark);
                     })) {
            yield return structured;
        }
    }

    private static IEnumerable<NativeEventFull> ReadFullIterator(
        NativeEventQuery query,
        CancellationToken cancellationToken) {

        using var systemRenderer = new WindowsEventSystemRenderer();
        using var messageRenderer = new WindowsEventMessageRenderer(
            query.Session,
            query.PublisherMetadataPath,
            query.MessageLocale,
            query.FallbackMessageLocale);
        using var payloadRenderer = new WindowsEventPayloadRenderer();
        foreach (NativeEventFull full in ReadEvents(
                     query,
                     cancellationToken,
                     eventHandle => {
                         NativeEventMetadata metadata =
                             systemRenderer.Render(eventHandle);
                         NativeEventMessage message = messageRenderer.Render(eventHandle, metadata, includeBookmark: false);
                         NativeEventStructured structured =
                             payloadRenderer.Render(
                                 eventHandle,
                                 metadata,
                                 query.IncludeBookmark);
                         return new NativeEventFull(message, structured);
                     })) {
            yield return full;
        }
    }

    private static IEnumerable<T> ReadEvents<T>(
        NativeEventQuery eventQuery,
        CancellationToken cancellationToken,
        Func<IntPtr, T> projector) {

        using var events = new WindowsEventHandleEnumerator(
            eventQuery,
            cancellationToken);
        while (events.MoveNext()) {
            T result = projector(events.Current);
            events.ReleaseCurrent();
            yield return result;
        }
    }
}

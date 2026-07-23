using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace EventViewerX.Native;

internal static class WindowsEventReader {
    private const int BatchSize = 64;

    internal static IEnumerable<EventObject> Read(
        NativeEventQuery query,
        EventReadMode readMode,
        string queriedMachine,
        string containerLog,
        CancellationToken cancellationToken) {

        switch (readMode) {
            case EventReadMode.Metadata:
                foreach (NativeEventMetadata metadata in ReadMetadata(query, cancellationToken)) {
                    yield return new EventObject(metadata, queriedMachine, containerLog);
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

    private static IEnumerable<NativeEventMessage> ReadMessagesIterator(
        NativeEventQuery query,
        CancellationToken cancellationToken) {

        using var systemRenderer = new WindowsEventSystemRenderer();
        using var messageRenderer = new WindowsEventMessageRenderer(
            query.PublisherMetadataPath,
            query.MessageLocale);
        foreach (NativeEventMessage message in ReadEvents(
                     query,
                     cancellationToken,
                     eventHandle => {
                         NativeEventMetadata metadata = systemRenderer.Render(eventHandle);
                         return messageRenderer.Render(eventHandle, metadata);
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
                         NativeEventMetadata metadata = systemRenderer.Render(eventHandle);
                         return payloadRenderer.Render(eventHandle, metadata);
                     })) {
            yield return structured;
        }
    }

    private static IEnumerable<NativeEventFull> ReadFullIterator(
        NativeEventQuery query,
        CancellationToken cancellationToken) {

        using var systemRenderer = new WindowsEventSystemRenderer();
        using var messageRenderer = new WindowsEventMessageRenderer(
            query.PublisherMetadataPath,
            query.MessageLocale);
        using var payloadRenderer = new WindowsEventPayloadRenderer();
        foreach (NativeEventFull full in ReadEvents(
                     query,
                     cancellationToken,
                     eventHandle => {
                         NativeEventMetadata metadata = systemRenderer.Render(eventHandle);
                         NativeEventMessage message = messageRenderer.Render(eventHandle, metadata, includeBookmark: false);
                         NativeEventStructured structured = payloadRenderer.Render(eventHandle, metadata);
                         return new NativeEventFull(message, structured);
                     })) {
            yield return full;
        }
    }

    private static IEnumerable<T> ReadEvents<T>(
        NativeEventQuery eventQuery,
        CancellationToken cancellationToken,
        Func<IntPtr, T> projector) {

        using WindowsEventNativeMethods.EventHandle query = WindowsEventNativeMethods.EvtQuery(
            eventQuery.Session,
            eventQuery.Path,
            eventQuery.XPath,
            eventQuery.Flags);
        if (query.IsInvalid) {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                $"Failed to query Windows event source '{eventQuery.DisplayName}'.");
        }

        var handles = new IntPtr[BatchSize];

        while (true) {
            cancellationToken.ThrowIfCancellationRequested();
            Array.Clear(handles, 0, handles.Length);
            if (!WindowsEventNativeMethods.EvtNext(
                    query,
                    handles.Length,
                    handles,
                    -1,
                    0,
                    out int returned)) {

                int error = Marshal.GetLastWin32Error();
                if (error == WindowsEventNativeMethods.ErrorNoMoreItems) {
                    yield break;
                }
                throw new Win32Exception(
                    error,
                    $"Failed while reading Windows event source '{eventQuery.DisplayName}'.");
            }

            int index = 0;
            try {
                for (; index < returned; index++) {
                    cancellationToken.ThrowIfCancellationRequested();
                    IntPtr eventHandle = handles[index];
                    T result;
                    try {
                        result = projector(eventHandle);
                    } finally {
                        WindowsEventNativeMethods.EvtClose(eventHandle);
                        handles[index] = IntPtr.Zero;
                    }
                    yield return result;
                }
            } finally {
                for (; index < returned; index++) {
                    if (handles[index] != IntPtr.Zero) {
                        WindowsEventNativeMethods.EvtClose(handles[index]);
                        handles[index] = IntPtr.Zero;
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace EventViewerX.Native;

internal static class WindowsEventFileReader {
    private const int BatchSize = 64;

    internal static IEnumerable<NativeEventMetadata> ReadMetadata(
        string filePath,
        string xpath,
        bool oldest,
        CancellationToken cancellationToken) {

        return ReadMetadataIterator(filePath, xpath, oldest, cancellationToken);
    }

    private static IEnumerable<NativeEventMetadata> ReadMetadataIterator(
        string filePath,
        string xpath,
        bool oldest,
        CancellationToken cancellationToken) {

        using var renderer = new WindowsEventSystemRenderer();
        foreach (NativeEventMetadata metadata in ReadEvents(
                     filePath,
                     xpath,
                     oldest,
                     cancellationToken,
                     renderer.Render)) {
            yield return metadata;
        }
    }

    internal static IEnumerable<NativeEventMessage> ReadMessages(
        string filePath,
        string xpath,
        bool oldest,
        CancellationToken cancellationToken) {

        return ReadMessagesIterator(filePath, xpath, oldest, cancellationToken);
    }

    internal static IEnumerable<NativeEventStructured> ReadStructured(
        string filePath,
        string xpath,
        bool oldest,
        CancellationToken cancellationToken) {

        return ReadStructuredIterator(filePath, xpath, oldest, cancellationToken);
    }

    internal static IEnumerable<NativeEventFull> ReadFull(
        string filePath,
        string xpath,
        bool oldest,
        CancellationToken cancellationToken) {

        return ReadFullIterator(filePath, xpath, oldest, cancellationToken);
    }

    private static IEnumerable<NativeEventMessage> ReadMessagesIterator(
        string filePath,
        string xpath,
        bool oldest,
        CancellationToken cancellationToken) {

        using var systemRenderer = new WindowsEventSystemRenderer();
        using var messageRenderer = new WindowsEventMessageRenderer(filePath);
        foreach (NativeEventMessage message in ReadEvents(
                     filePath,
                     xpath,
                     oldest,
                     cancellationToken,
                     eventHandle => {
                         NativeEventMetadata metadata = systemRenderer.Render(eventHandle);
                         return messageRenderer.Render(eventHandle, metadata);
                     })) {
            yield return message;
        }
    }

    private static IEnumerable<NativeEventStructured> ReadStructuredIterator(
        string filePath,
        string xpath,
        bool oldest,
        CancellationToken cancellationToken) {

        using var systemRenderer = new WindowsEventSystemRenderer();
        using var payloadRenderer = new WindowsEventPayloadRenderer();
        foreach (NativeEventStructured structured in ReadEvents(
                     filePath,
                     xpath,
                     oldest,
                     cancellationToken,
                     eventHandle => {
                         NativeEventMetadata metadata = systemRenderer.Render(eventHandle);
                         return payloadRenderer.Render(eventHandle, metadata);
                     })) {
            yield return structured;
        }
    }

    private static IEnumerable<NativeEventFull> ReadFullIterator(
        string filePath,
        string xpath,
        bool oldest,
        CancellationToken cancellationToken) {

        using var systemRenderer = new WindowsEventSystemRenderer();
        using var messageRenderer = new WindowsEventMessageRenderer(filePath);
        using var payloadRenderer = new WindowsEventPayloadRenderer();
        foreach (NativeEventFull full in ReadEvents(
                     filePath,
                     xpath,
                     oldest,
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
        string filePath,
        string xpath,
        bool oldest,
        CancellationToken cancellationToken,
        Func<IntPtr, T> projector) {

        WindowsEventNativeMethods.QueryFlags flags = WindowsEventNativeMethods.QueryFlags.FilePath |
            (oldest
                ? WindowsEventNativeMethods.QueryFlags.ForwardDirection
                : WindowsEventNativeMethods.QueryFlags.ReverseDirection);
        using WindowsEventNativeMethods.EventHandle query = WindowsEventNativeMethods.EvtQuery(
            IntPtr.Zero,
            filePath,
            xpath,
            flags);
        if (query.IsInvalid) {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to query EVTX file '{filePath}'.");
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
                throw new Win32Exception(error, $"Failed while reading EVTX file '{filePath}'.");
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

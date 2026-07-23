using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Runtime.InteropServices;

namespace EventViewerX.Native;

internal sealed class WindowsEventMessageRenderer : IDisposable {
    private readonly string? _filePath;
    private readonly IntPtr _session;
    private readonly int _locale;
    private readonly string _cultureName;
    private readonly NativeEventBuffer _messageBuffer = new();
    private readonly WindowsEventBookmarkRenderer _bookmarkRenderer = new();
    private readonly Dictionary<string, ProviderContext> _providers =
        new(StringComparer.OrdinalIgnoreCase);

    internal WindowsEventMessageRenderer(
        IntPtr session,
        string? filePath,
        int locale = 0) {

        _session = session;
        _filePath = filePath;
        CultureInfo culture = locale == 0
            ? CultureInfo.CurrentUICulture
            : CultureInfo.GetCultureInfo(locale);
        _locale = culture.LCID;
        _cultureName = culture.Name;
    }

    internal NativeEventMessage Render(
        IntPtr eventHandle,
        NativeEventMetadata metadata,
        bool includeBookmark = true) {
        ProviderContext provider = GetProvider(metadata.ProviderName);
        FormatResult message = Format(
            provider,
            eventHandle,
            WindowsEventNativeMethods.FormatMessageFlags.Event);
        string level = provider.GetLevel(
            metadata.Level,
            () => Format(provider, eventHandle, WindowsEventNativeMethods.FormatMessageFlags.Level).Text);
        string task = provider.GetTask(
            metadata.Task,
            () => Format(provider, eventHandle, WindowsEventNativeMethods.FormatMessageFlags.Task).Text);
        string opcode = provider.GetOpcode(
            metadata.Task,
            metadata.Opcode,
            () => Format(provider, eventHandle, WindowsEventNativeMethods.FormatMessageFlags.Opcode).Text);
        IReadOnlyList<string> keywords = provider.GetKeywords(
            metadata.Keywords,
            () => SplitKeywords(Format(
                provider,
                eventHandle,
                WindowsEventNativeMethods.FormatMessageFlags.Keyword).Text));

        if (string.IsNullOrEmpty(level)) {
            level = metadata.Level switch {
                1 => "Critical",
                2 => "Error",
                3 => "Warning",
                4 => "Information",
                5 => "Verbose",
                _ => metadata.Level?.ToString() ?? string.Empty
            };
        }

        return new NativeEventMessage(
            metadata,
            message.Text,
            level,
            task,
            opcode,
            keywords,
            includeBookmark ? _bookmarkRenderer.Render(eventHandle) : null,
            _cultureName,
            GetRenderStatus(provider, message.ErrorCode),
            message.ErrorCode);
    }

    private ProviderContext GetProvider(string providerName) {
        if (_providers.TryGetValue(providerName, out ProviderContext? provider)) {
            return provider;
        }

        WindowsEventNativeMethods.EventHandle handle = WindowsEventNativeMethods.EvtOpenPublisherMetadata(
            _session,
            providerName,
            _filePath,
            _locale,
            0);
        int openError = handle.IsInvalid ? Marshal.GetLastWin32Error() : 0;
        if (handle.IsInvalid) {
            handle.Dispose();
            handle = WindowsEventNativeMethods.EvtOpenPublisherMetadata(
                _session,
                providerName,
                null,
                _locale,
                0);
            openError = handle.IsInvalid ? Marshal.GetLastWin32Error() : 0;
        }

        provider = new ProviderContext(handle, openError);
        _providers.Add(providerName, provider);
        return provider;
    }

    private FormatResult Format(
        ProviderContext provider,
        IntPtr eventHandle,
        WindowsEventNativeMethods.FormatMessageFlags flags) {

        if (provider.Handle.IsInvalid) {
            return new FormatResult(string.Empty, provider.OpenErrorCode);
        }

        IntPtr publisherHandle = provider.Handle.DangerousGetHandle();
        if (!WindowsEventNativeMethods.EvtFormatMessage(
                publisherHandle,
                eventHandle,
                0,
                0,
                IntPtr.Zero,
                flags,
                _messageBuffer.Capacity / sizeof(char),
                _messageBuffer.Pointer,
                out int bufferUsed)) {

            int error = Marshal.GetLastWin32Error();
            if (error != WindowsEventNativeMethods.ErrorInsufficientBuffer) {
                return new FormatResult(string.Empty, error);
            }

            _messageBuffer.EnsureCapacity(checked(bufferUsed * sizeof(char)));
            if (!WindowsEventNativeMethods.EvtFormatMessage(
                    publisherHandle,
                    eventHandle,
                    0,
                    0,
                    IntPtr.Zero,
                    flags,
                    _messageBuffer.Capacity / sizeof(char),
                    _messageBuffer.Pointer,
                    out bufferUsed)) {
                return new FormatResult(string.Empty, Marshal.GetLastWin32Error());
            }
        }

        if (bufferUsed <= 0) {
            return new FormatResult(string.Empty, 0);
        }

        return new FormatResult(
            (Marshal.PtrToStringUni(_messageBuffer.Pointer, bufferUsed) ?? string.Empty)
                .TrimEnd('\0'),
            0);
    }

    private static EventMessageRenderStatus GetRenderStatus(
        ProviderContext provider,
        int errorCode) {

        if (errorCode == 0) {
            return EventMessageRenderStatus.Rendered;
        }
        if (provider.Handle.IsInvalid ||
            errorCode == WindowsEventNativeMethods.ErrorEvtPublisherMetadataNotFound) {
            return EventMessageRenderStatus.ProviderMetadataUnavailable;
        }
        if (errorCode == WindowsEventNativeMethods.ErrorEvtMessageNotFound ||
            errorCode == WindowsEventNativeMethods.ErrorEvtMessageIdNotFound ||
            errorCode == WindowsEventNativeMethods.ErrorEvtMessageLocaleNotFound) {
            return EventMessageRenderStatus.MessageResourceUnavailable;
        }
        return EventMessageRenderStatus.Failed;
    }

    private static IReadOnlyList<string> SplitKeywords(string formattedKeywords) {
        if (string.IsNullOrEmpty(formattedKeywords)) {
            return Array.Empty<string>();
        }

        return formattedKeywords.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
    }

    public void Dispose() {
        foreach (ProviderContext provider in _providers.Values) {
            provider.Dispose();
        }
        _providers.Clear();
        _messageBuffer.Dispose();
        _bookmarkRenderer.Dispose();
    }

    private readonly struct FormatResult {
        internal FormatResult(string text, int errorCode) {
            Text = text;
            ErrorCode = errorCode;
        }

        internal string Text { get; }
        internal int ErrorCode { get; }
    }

    private sealed class ProviderContext : IDisposable {
        private readonly Dictionary<byte, string> _levels = new();
        private readonly Dictionary<int, string> _tasks = new();
        private readonly Dictionary<long, string> _opcodes = new();
        private readonly Dictionary<long, IReadOnlyList<string>> _keywords = new();

        internal ProviderContext(
            WindowsEventNativeMethods.EventHandle handle,
            int openErrorCode) {

            Handle = handle;
            OpenErrorCode = openErrorCode;
        }

        internal WindowsEventNativeMethods.EventHandle Handle { get; }
        internal int OpenErrorCode { get; }

        internal string GetLevel(byte? value, Func<string> factory) {
            return value.HasValue ? GetOrAdd(_levels, value.Value, factory) : string.Empty;
        }

        internal string GetTask(int? value, Func<string> factory) {
            return value.HasValue ? GetOrAdd(_tasks, value.Value, factory) : string.Empty;
        }

        internal string GetOpcode(int? task, short? opcode, Func<string> factory) {
            if (!opcode.HasValue) {
                return string.Empty;
            }
            long key = ((long)(task ?? 0) << 32) | (uint)(ushort)opcode.Value;
            return GetOrAdd(_opcodes, key, factory);
        }

        internal IReadOnlyList<string> GetKeywords(long? value, Func<IReadOnlyList<string>> factory) {
            return value.HasValue ? GetOrAdd(_keywords, value.Value, factory) : Array.Empty<string>();
        }

        private static TValue GetOrAdd<TKey, TValue>(
            Dictionary<TKey, TValue> cache,
            TKey key,
            Func<TValue> factory)
            where TKey : notnull {

            if (cache.TryGetValue(key, out TValue? value)) {
                return value;
            }
            value = factory();
            cache.Add(key, value);
            return value;
        }

        public void Dispose() {
            Handle.Dispose();
        }
    }
}

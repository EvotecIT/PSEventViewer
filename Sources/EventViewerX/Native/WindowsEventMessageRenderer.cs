using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Runtime.InteropServices;

namespace EventViewerX.Native;

internal sealed class WindowsEventMessageRenderer : IDisposable {
    private readonly string? _filePath;
    private readonly int _locale;
    private readonly string _cultureName;
    private readonly NativeEventBuffer _messageBuffer = new();
    private readonly WindowsEventBookmarkRenderer _bookmarkRenderer = new();
    private readonly Dictionary<string, ProviderContext> _providers =
        new(StringComparer.OrdinalIgnoreCase);

    internal WindowsEventMessageRenderer(string? filePath, int locale = 0) {
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
        string message = Format(provider, eventHandle, WindowsEventNativeMethods.FormatMessageFlags.Event);
        string level = provider.GetLevel(
            metadata.Level,
            () => Format(provider, eventHandle, WindowsEventNativeMethods.FormatMessageFlags.Level));
        string task = provider.GetTask(
            metadata.Task,
            () => Format(provider, eventHandle, WindowsEventNativeMethods.FormatMessageFlags.Task));
        string opcode = provider.GetOpcode(
            metadata.Task,
            metadata.Opcode,
            () => Format(provider, eventHandle, WindowsEventNativeMethods.FormatMessageFlags.Opcode));
        IReadOnlyList<string> keywords = provider.GetKeywords(
            metadata.Keywords,
            () => SplitKeywords(Format(provider, eventHandle, WindowsEventNativeMethods.FormatMessageFlags.Keyword)));

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
            message,
            level,
            task,
            opcode,
            keywords,
            includeBookmark ? _bookmarkRenderer.Render(eventHandle) : null,
            _cultureName);
    }

    private ProviderContext GetProvider(string providerName) {
        if (_providers.TryGetValue(providerName, out ProviderContext? provider)) {
            return provider;
        }

        WindowsEventNativeMethods.EventHandle handle = WindowsEventNativeMethods.EvtOpenPublisherMetadata(
            IntPtr.Zero,
            providerName,
            _filePath,
            _locale,
            0);
        if (handle.IsInvalid) {
            handle.Dispose();
            handle = WindowsEventNativeMethods.EvtOpenPublisherMetadata(
                IntPtr.Zero,
                providerName,
                null,
                _locale,
                0);
        }

        provider = new ProviderContext(handle);
        _providers.Add(providerName, provider);
        return provider;
    }

    private string Format(
        ProviderContext provider,
        IntPtr eventHandle,
        WindowsEventNativeMethods.FormatMessageFlags flags) {

        if (provider.Handle.IsInvalid) {
            return string.Empty;
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
                return string.Empty;
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
                return string.Empty;
            }
        }

        if (bufferUsed <= 0) {
            return string.Empty;
        }

        return (Marshal.PtrToStringUni(_messageBuffer.Pointer, bufferUsed) ?? string.Empty)
            .TrimEnd('\0');
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

    private sealed class ProviderContext : IDisposable {
        private readonly Dictionary<byte, string> _levels = new();
        private readonly Dictionary<int, string> _tasks = new();
        private readonly Dictionary<long, string> _opcodes = new();
        private readonly Dictionary<long, IReadOnlyList<string>> _keywords = new();

        internal ProviderContext(WindowsEventNativeMethods.EventHandle handle) {
            Handle = handle;
        }

        internal WindowsEventNativeMethods.EventHandle Handle { get; }

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

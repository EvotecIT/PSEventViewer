using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace EventViewerX.Native;

internal sealed class WindowsEventHandleEnumerator : IDisposable {
    private const int BatchSize = 64;
    private readonly CancellationToken _cancellationToken;
    private readonly NativeEventQuery _eventQuery;
    private readonly IntPtr[] _handles = new IntPtr[BatchSize];
    private readonly WindowsEventNativeMethods.EventHandle _query;
    private int _index;
    private int _returned;
    private bool _disposed;

    internal WindowsEventHandleEnumerator(
        NativeEventQuery eventQuery,
        CancellationToken cancellationToken) {

        _eventQuery = eventQuery;
        _cancellationToken = cancellationToken;
        _query = WindowsEventNativeMethods.EvtQuery(
            eventQuery.Session,
            eventQuery.Path,
            eventQuery.XPath,
            eventQuery.Flags);
        if (_query.IsInvalid) {
            int error = Marshal.GetLastWin32Error();
            _query.Dispose();
            throw new Win32Exception(
                error,
                $"Failed to query Windows event source '{eventQuery.DisplayName}'.");
        }
    }

    internal IntPtr Current { get; private set; }

    internal void ReleaseCurrent() {
        CloseCurrent();
    }

    internal bool MoveNext() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(WindowsEventHandleEnumerator));
        }
        _cancellationToken.ThrowIfCancellationRequested();
        CloseCurrent();

        if (_index >= _returned && !ReadBatch()) {
            return false;
        }

        Current = _handles[_index];
        _handles[_index] = IntPtr.Zero;
        _index++;
        return true;
    }

    private bool ReadBatch() {
        Array.Clear(_handles, 0, _handles.Length);
        _index = 0;
        _returned = 0;
        if (WindowsEventNativeMethods.EvtNext(
                _query,
                _handles.Length,
                _handles,
                _eventQuery.NextTimeoutMilliseconds > 0
                    ? _eventQuery.NextTimeoutMilliseconds
                    : -1,
                0,
                out _returned)) {
            return _returned > 0;
        }

        int error = Marshal.GetLastWin32Error();
        if (error == WindowsEventNativeMethods.ErrorNoMoreItems) {
            return false;
        }
        if (error == WindowsEventNativeMethods.ErrorTimeout) {
            throw new TimeoutException(
                $"Timed out while reading Windows event source '{_eventQuery.DisplayName}'.");
        }
        throw new Win32Exception(
            error,
            $"Failed while reading Windows event source '{_eventQuery.DisplayName}'.");
    }

    private void CloseCurrent() {
        if (Current != IntPtr.Zero) {
            WindowsEventNativeMethods.EvtClose(Current);
            Current = IntPtr.Zero;
        }
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        CloseCurrent();
        for (int index = _index; index < _returned; index++) {
            if (_handles[index] != IntPtr.Zero) {
                WindowsEventNativeMethods.EvtClose(_handles[index]);
                _handles[index] = IntPtr.Zero;
            }
        }
        _query.Dispose();
    }
}

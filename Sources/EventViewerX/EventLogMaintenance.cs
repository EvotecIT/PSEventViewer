using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using EventViewerX.Native;

namespace EventViewerX;

/// <summary>Native Windows Event Log maintenance operations.</summary>
public static class EventLogMaintenance {
    /// <summary>Clears a local or remote channel and optionally saves its prior records.</summary>
    public static EventLogClearResult ClearChannel(
        string logName,
        string? machineName = null,
        string? backupPath = null,
        NetworkCredential? credential = null,
        EventLogAuthentication authentication =
            EventLogAuthentication.Default,
        int remoteConnectionTimeoutMilliseconds = 5000,
        CancellationToken cancellationToken = default) {

        if (string.IsNullOrWhiteSpace(logName)) {
            throw new ArgumentException(
                "Event log name cannot be null or empty.",
                nameof(logName));
        }
        if (!Enum.IsDefined(
                typeof(EventLogAuthentication),
                authentication)) {
            throw new ArgumentOutOfRangeException(nameof(authentication));
        }
        if (remoteConnectionTimeoutMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(remoteConnectionTimeoutMilliseconds),
                "Remote connection timeout must be greater than zero.");
        }
        cancellationToken.ThrowIfCancellationRequested();
        string machine = machineName?.Trim() ?? string.Empty;
        bool remote = !EventLogTarget.IsLocalMachine(machine);
        if (!remote && credential != null) {
            throw new ArgumentException(
                "Credentials can only be used for a remote clear operation.",
                nameof(credential));
        }
        string? normalizedBackup = null;
        if (!string.IsNullOrWhiteSpace(backupPath)) {
            string requestedBackup = backupPath!.Trim().Trim('"', '\'');
            normalizedBackup = remote
                ? requestedBackup
                : Path.GetFullPath(requestedBackup);
        }
        if (!remote && normalizedBackup != null) {
            string? directory = Path.GetDirectoryName(normalizedBackup);
            if (string.IsNullOrWhiteSpace(directory) ||
                !Directory.Exists(directory)) {
                throw new DirectoryNotFoundException(
                    $"Backup directory '{directory}' does not exist.");
            }
            if (File.Exists(normalizedBackup)) {
                throw new IOException(
                    $"Backup file '{normalizedBackup}' already exists.");
            }
        }

        WindowsEventNativeMethods.EventHandle? session = null;
        CancellationTokenRegistration cancellationRegistration =
            default;
        try {
            IntPtr sessionHandle = IntPtr.Zero;
            if (remote) {
                session = WindowsEventRemoteSession.OpenBounded(
                    machine,
                    credential,
                    authentication,
                    remoteConnectionTimeoutMilliseconds,
                    cancellationToken);
                sessionHandle = session.DangerousGetHandle();
                if (cancellationToken.CanBeCanceled) {
                    cancellationRegistration =
                        cancellationToken.Register(
                            static state =>
                                WindowsEventNativeMethods.EvtCancel(
                                    (WindowsEventNativeMethods
                                        .EventHandle)state!),
                            session);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (!WindowsEventNativeMethods.EvtClearLog(
                    sessionHandle,
                    logName.Trim(),
                    normalizedBackup,
                    0)) {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(
                    error,
                    $"Failed to clear Windows event channel '{logName}' on '{(remote ? machine : Environment.MachineName)}'.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            return new EventLogClearResult(
                logName.Trim(),
                remote ? machine : null,
                normalizedBackup);
        } finally {
            cancellationRegistration.Dispose();
            session?.Dispose();
        }
    }
}

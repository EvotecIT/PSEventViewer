using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using Xunit;

namespace EventViewerX.Tests;

public class TestEventLogDetailsResult {
    [Fact]
    public void GetLogDetailsResult_KnownLog_ReturnsDetails() {
        if (!OperatingSystem.IsWindows()) return;

        EventLogDetailsResult result = SearchEvents.GetLogDetailsResult("Application");

        Assert.True(result.Success);
        Assert.Equal(EventLogDetailsStatus.Success, result.Status);
        Assert.NotNull(result.Details);
        Assert.Equal("Application", result.LogName);
    }

    [Fact]
    public void GetLogDetailsResult_MissingLog_ReturnsDiagnosticFailure() {
        if (!OperatingSystem.IsWindows()) return;

        EventLogDetailsResult result = SearchEvents.GetLogDetailsResult("Definitely-Missing-EventViewerX-UnitTest-Log");

        Assert.False(result.Success);
        Assert.Equal(EventLogDetailsStatus.LogConfigurationUnavailable, result.Status);
        Assert.Equal("Definitely-Missing-EventViewerX-UnitTest-Log", result.LogName);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorType));
    }

    [Fact]
    public void GetLogDetailsResult_ExistingSession_ReusesSession() {
        if (!OperatingSystem.IsWindows()) return;

        using var session = new EventLogSession();

        EventLogDetailsResult result = SearchEvents.GetLogDetailsResult(
            "Application",
            session,
            timeoutMs: 10000,
            machineName: Environment.MachineName);

        Assert.True(result.Success);
        Assert.Equal(EventLogDetailsStatus.Success, result.Status);
        Assert.NotNull(result.Details);
        Assert.Equal(Environment.MachineName, result.MachineName);
    }

    [Fact]
    public void OpenSessionResult_LocalMachine_ReturnsSession() {
        if (!OperatingSystem.IsWindows()) return;

        using EventLogSessionOpenResult result = SearchEvents.OpenSessionResult(null, purpose: "UnitTest", logName: "Application");

        Assert.True(result.Success);
        Assert.Equal(EventLogSessionOpenStatus.Success, result.Status);
        Assert.NotNull(result.Session);
        Assert.Equal("UnitTest", result.Purpose);
        Assert.Equal("Application", result.LogName);
    }

    [Fact]
    public void MapSessionFailureStatus_SessionConstructionFailureIsHostUnavailable() {
        Assert.Equal(
            EventLogDetailsStatus.HostUnavailable,
            SearchEvents.MapSessionFailureStatus(EventLogSessionOpenStatus.EventLogSessionUnavailable));
    }

    [Fact]
    public void DiagnosticProperties_DistinguishPartialFailureFromCleanSuccess() {
        var result = new EventLogDetailsResult {
            LogName = "System",
            MachineName = "AD1",
            Status = EventLogDetailsStatus.EventTimesUnavailable,
            ErrorMessage = "Newest event could not be read."
        };

        Assert.True(result.HasDiagnosticFailure);
        Assert.Contains("System", result.DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains("AD1", result.DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains(nameof(EventLogDetailsStatus.EventTimesUnavailable), result.DiagnosticMessage, StringComparison.Ordinal);
        Assert.Contains("Newest event could not be read.", result.DiagnosticMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyEventTimeFailure_ReportsTimeoutWithPartialDetailsStatus() {
        var result = new EventLogDetailsResult { Status = EventLogDetailsStatus.Success };

        SearchEvents.ApplyEventTimeFailure(result, new TimeoutException("event-time budget expired"));

        Assert.Equal(EventLogDetailsStatus.Timeout, result.Status);
        Assert.Equal(nameof(TimeoutException), result.ErrorType);
        Assert.Contains("event-time budget expired", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyEventTimeFailure_ReportsNonTimeoutReadFailure() {
        var result = new EventLogDetailsResult { Status = EventLogDetailsStatus.Success };

        SearchEvents.ApplyEventTimeFailure(result, new InvalidOperationException("event-time read failed"));

        Assert.Equal(EventLogDetailsStatus.EventTimesUnavailable, result.Status);
        Assert.Equal(nameof(InvalidOperationException), result.ErrorType);
        Assert.Contains("event-time read failed", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyEventTimeFailure_PreservesAccessDeniedClassification() {
        var result = new EventLogDetailsResult { Status = EventLogDetailsStatus.Success };

        SearchEvents.ApplyEventTimeFailure(result, new UnauthorizedAccessException("event-time access denied"));

        Assert.Equal(EventLogDetailsStatus.AccessDenied, result.Status);
        Assert.Equal(nameof(UnauthorizedAccessException), result.ErrorType);
    }

    [Fact]
    public void ApplyEventTimeFailure_PreservesEarlierPartialStatusAndAppendsDiagnostic() {
        var result = new EventLogDetailsResult {
            Status = EventLogDetailsStatus.Timeout,
            ErrorMessage = "Runtime information was unavailable.",
            ErrorType = nameof(TimeoutException)
        };

        SearchEvents.ApplyEventTimeFailure(result, new InvalidOperationException("event-time read failed"));

        Assert.Equal(EventLogDetailsStatus.Timeout, result.Status);
        Assert.Equal($"{nameof(TimeoutException)};{nameof(InvalidOperationException)}", result.ErrorType);
        Assert.Contains("Runtime information was unavailable.", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("event-time read failed", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void MapLogInformationFailureStatus_PreservesActionableClassification() {
        Assert.Equal(
            EventLogDetailsStatus.Timeout,
            SearchEvents.MapLogInformationFailureStatus(new TimeoutException()));
        Assert.Equal(
            EventLogDetailsStatus.AccessDenied,
            SearchEvents.MapLogInformationFailureStatus(new UnauthorizedAccessException()));
        Assert.Equal(
            EventLogDetailsStatus.LogInformationUnavailable,
            SearchEvents.MapLogInformationFailureStatus(new InvalidOperationException()));
    }

    [Fact]
    public void WriteLogDetailsWarningIfNeeded_WarnsForPartialStatusOnly() {
        InternalLogger previous = Settings._logger;
        var logger = new InternalLogger();
        var warnings = new List<string>();
        logger.OnWarningMessage += (_, args) => warnings.Add(args.FullMessage);
        Settings._logger = logger;
        try {
            SearchEvents.WriteLogDetailsWarningIfNeeded(new EventLogDetailsResult {
                LogName = "System",
                Status = EventLogDetailsStatus.EventTimesUnavailable,
                ErrorMessage = "Timestamp read failed."
            });
            SearchEvents.WriteLogDetailsWarningIfNeeded(new EventLogDetailsResult {
                LogName = "Application",
                Status = EventLogDetailsStatus.Success
            });
        } finally {
            Settings._logger = previous;
        }

        string warning = Assert.Single(warnings);
        Assert.Contains("System", warning, StringComparison.Ordinal);
        Assert.Contains(nameof(EventLogDetailsStatus.EventTimesUnavailable), warning, StringComparison.Ordinal);
    }

    [Fact]
    public void MergeDiagnosticStatus_PromotesGenericFailureButRetainsActionableFailure() {
        Assert.Equal(
            EventLogDetailsStatus.Timeout,
            SearchEvents.MergeDiagnosticStatus(
                EventLogDetailsStatus.LogInformationUnavailable,
                EventLogDetailsStatus.Timeout));
        Assert.Equal(
            EventLogDetailsStatus.AccessDenied,
            SearchEvents.MergeDiagnosticStatus(
                EventLogDetailsStatus.AccessDenied,
                EventLogDetailsStatus.Timeout));
        Assert.Equal(
            EventLogDetailsStatus.LogConfigurationUnavailable,
            SearchEvents.MergeDiagnosticStatus(
                EventLogDetailsStatus.LogConfigurationUnavailable,
                EventLogDetailsStatus.EventTimesUnavailable));
    }

    [Fact]
    public void AppendResultDiagnostic_PreservesAllPartialFailureEvidence() {
        var result = new EventLogDetailsResult { Status = EventLogDetailsStatus.LogInformationUnavailable };

        SearchEvents.AppendResultDiagnostic(
            result,
            EventLogDetailsStatus.Timeout,
            "Runtime information timed out.",
            nameof(TimeoutException));
        SearchEvents.AppendResultDiagnostic(
            result,
            EventLogDetailsStatus.EventTimesUnavailable,
            "Event time failed.",
            nameof(InvalidOperationException));

        Assert.Equal(EventLogDetailsStatus.Timeout, result.Status);
        Assert.Contains("Runtime information timed out.", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("Event time failed.", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Equal($"{nameof(TimeoutException)};{nameof(InvalidOperationException)}", result.ErrorType);
    }

    [Fact]
    public void DetailsOnlyCompatibilityApiOwnsWarningButTypedApiDoesNot() {
        if (!OperatingSystem.IsWindows()) return;

        InternalLogger previous = Settings._logger;
        var logger = new InternalLogger();
        var warnings = new List<string>();
        logger.OnWarningMessage += (_, args) => warnings.Add(args.FullMessage);
        Settings._logger = logger;
        try {
            EventLogDetailsResult typed = SearchEvents.GetLogDetailsResult("Definitely-Missing-EventViewerX-Warning-Test-Log");
            Assert.True(typed.HasDiagnosticFailure);
            Assert.Empty(warnings);

            EventLogDetails? details = SearchEvents.GetLogDetails("Definitely-Missing-EventViewerX-Warning-Test-Log");
            Assert.Null(details);
        } finally {
            Settings._logger = previous;
        }

        string warning = Assert.Single(warnings);
        Assert.Contains("Definitely-Missing-EventViewerX-Warning-Test-Log", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void TypedSessionResultCanSuppressBoundaryDiagnostics() {
        if (!OperatingSystem.IsWindows()) return;

        InternalLogger previous = Settings._logger;
        var logger = new InternalLogger();
        var warnings = new List<string>();
        logger.OnWarningMessage += (_, args) => warnings.Add(args.FullMessage);
        Settings._logger = logger;
        try {
            using EventLogSessionOpenResult result = SearchEvents.CreateSessionResult(
                null,
                "LogDetails",
                "Application",
                timeoutMs: 1000,
                localSessionFactory: static () => throw new InvalidOperationException("Expected session failure."),
                emitDiagnostics: false);

            Assert.False(result.Success);
            Assert.Equal(EventLogSessionOpenStatus.LocalSessionUnavailable, result.Status);
            Assert.Empty(warnings);
        } finally {
            Settings._logger = previous;
        }
    }
}

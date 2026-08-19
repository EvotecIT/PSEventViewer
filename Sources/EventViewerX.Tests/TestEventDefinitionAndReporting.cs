using System.Diagnostics.Eventing.Reader;
using System.IO.Compression;
using System.Security.Principal;
using System.Xml.Linq;
using EventViewerX.Reporting;
using EventViewerX.Rules.ActiveDirectory;
using Xunit;

namespace EventViewerX.Tests;

public sealed class TestEventDefinitionAndReporting {
    [Fact]
    public void CustomDefinitionRoundTripsCompilesAndProjects() {
        EventDefinition definition = CreateDefinition();
        string path = Path.Combine(Path.GetTempPath(), $"evx-definition-{Guid.NewGuid():N}.json");
        try {
            definition.Save(path);
            EventDefinition loaded = EventDefinition.Load(path);
            string xml = EventDefinitionCompiler.BuildQueryXml(loaded);
            Assert.Contains("Path=\"Security\"", xml, StringComparison.Ordinal);
            Assert.Contains("EventID=4625", xml, StringComparison.Ordinal);

            var source = new EventObject(new SyntheticEventRecord(), "WEC01", EventReadMode.StructuredDataAndMessage) {
                ContainerLog = "ForwardedEvents",
                GatheredLogName = "ForwardedEvents"
            };
            source.Data["TargetUserName"] = "alice";
            CustomEventRecord record = EventDefinitionEngine.CreateRecord(loaded, source);
            Assert.Equal("FailedLogonCustom", record.TypeName);
            Assert.Equal("alice", record.Values["User"]);
            Assert.Equal("source.ad.evotec.xyz", record.Values["Computer"]);
        } finally {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task OneSnapshotRendersHtmlExcelAndEmail() {
        var source = new EventObject(new SyntheticEventRecord(), "WEC01", EventReadMode.StructuredDataAndMessage) {
            ContainerLog = "ForwardedEvents",
            GatheredLogName = "ForwardedEvents"
        };
        source.Data["TargetUserName"] = "alice";
        CustomEventRecord custom = EventDefinitionEngine.CreateRecord(CreateDefinition(), source);
        EventReport report = EventReportEngine.Create(new object[] { custom }, "Failed logons");
        Assert.Single(report.Rows);
        EventReportSection customSection = Assert.Single(report.Sections);
        Assert.Equal(EventReportSectionKind.Custom, customSection.Kind);
        Assert.Equal(new[] { "User", "Computer" }, customSection.Columns.Select(static column => column.Name));
        Assert.DoesNotContain(customSection.Columns, static column => column.Name == nameof(EventReportRow.EventId));
        Assert.Equal("Security", report.Rows[0].SourceLog);
        Assert.Equal("ForwardedEvents", report.Rows[0].ContainerLog);

        string html = EventReportHtmlRenderer.Render(report);
        Assert.Contains("data-hfx-monitoring-shell=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hfx-monitoring-nav=\"overview\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hfx-monitoring-record-explorer=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hfx-monitoring-column-picker-toggle=\"true\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("hfx-report-workspace", html, StringComparison.Ordinal);
        Assert.Contains("Failed logons", html, StringComparison.Ordinal);
        Assert.Contains("alice", html, StringComparison.Ordinal);

        string workbook = Path.Combine(Path.GetTempPath(), $"evx-report-{Guid.NewGuid():N}.xlsx");
        try {
            Assert.Equal(workbook, EventReportExcelRenderer.Save(report, workbook));
            using ZipArchive archive = ZipFile.OpenRead(workbook);
            Assert.Contains(archive.Entries, static entry => entry.FullName == "xl/workbook.xml");
            Assert.Contains(archive.Entries, static entry => entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.Ordinal));
        } finally {
            File.Delete(workbook);
        }

        EventEmailPackage email = await EventReportEmailRenderer.RenderAsync(report);
        Assert.Contains("Failed logons", email.Subject, StringComparison.Ordinal);
        Assert.Contains("alice", email.Html, StringComparison.Ordinal);
        Assert.NotEmpty(email.PlainText);
    }

    [Fact]
    public async Task TypedReportsUseDefinitionFieldsAndKeepDifferentTypesInSeparateSections() {
        EventObject successfulSource = CreateSecuritySource(4624);
        successfulSource.Data["TargetDomainName"] = "EVOTEC";
        successfulSource.Data["TargetUserName"] = "alice";
        successfulSource.Data["SubjectDomainName"] = "EVOTEC";
        successfulSource.Data["SubjectUserName"] = "service.account";
        successfulSource.Data["IpAddress"] = "10.0.0.20";
        successfulSource.Data["IpPort"] = "55123";
        successfulSource.Data["LogonType"] = "3";

        EventObject failedSource = CreateSecuritySource(4625);
        failedSource.Data["TargetDomainName"] = "EVOTEC";
        failedSource.Data["TargetUserName"] = "bob";
        failedSource.Data["WorkstationName"] = "CLIENT01";
        failedSource.Data["IpAddress"] = "10.0.0.21";
        failedSource.Data["IpPort"] = "55124";
        failedSource.Data["FailureReason"] = "%%2304";

        EventReport report = EventReportEngine.Create(new object[] {
            new ADUserLogon(successfulSource),
            new ADUserLogonFailed(failedSource)
        }, "Authentication activity");

        Assert.Equal(2, report.Sections.Count);
        EventReportSection successful = Assert.Single(report.Sections,
            static section => section.Name == nameof(EventType.ADUserLogon));
        EventReportSection failed = Assert.Single(report.Sections,
            static section => section.Name == nameof(EventType.ADUserLogonFailed));
        Assert.Equal(EventReportSectionKind.Typed, successful.Kind);
        Assert.Contains(successful.Columns, static column => column.Name == "Who");
        Assert.Contains(successful.Columns, static column => column.Name == "When");
        Assert.Contains(successful.Columns, static column => column.Name == "IpAddress");
        Assert.Contains(successful.Columns, static column => column.DisplayName == "IP Address");
        Assert.DoesNotContain(successful.Columns, static column => column.Name is "EventId" or "Provider" or "EventIds" or "LogName" or "Type");
        Assert.Equal("EVOTEC\\alice", successful.Rows[0].Values["ObjectAffected"]);
        Assert.DoesNotContain(nameof(EventReportRow.EventId), successful.Rows[0].Values.Keys);
        Assert.Contains(failed.Columns, static column => column.Name == "FailureReason");

        string html = EventReportHtmlRenderer.Render(report);
        Assert.Contains("AD User Logon", html, StringComparison.Ordinal);
        Assert.Contains("AD User Logon Failed", html, StringComparison.Ordinal);
        Assert.Contains("Object Affected", html, StringComparison.Ordinal);
        Assert.Contains("EVOTEC\\alice", html, StringComparison.Ordinal);
        Assert.Contains("data-hfx-monitoring-nav=\"aduserlogon\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hfx-monitoring-nav=\"aduserlogonfailed\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hfx-monitoring-record-toggle", html, StringComparison.Ordinal);
        Assert.Contains("Source computer", html, StringComparison.Ordinal);
        Assert.Contains("ForwardedEvents", html, StringComparison.Ordinal);

        EventEmailPackage email = await EventReportEmailRenderer.RenderAsync(report);
        Assert.Contains("AD User Logon", email.Html, StringComparison.Ordinal);
        Assert.Contains("AD User Logon Failed", email.Html, StringComparison.Ordinal);
        Assert.Contains("EVOTEC\\alice", email.Html, StringComparison.Ordinal);

        string workbook = Path.Combine(Path.GetTempPath(), $"evx-typed-report-{Guid.NewGuid():N}.xlsx");
        try {
            EventReportExcelRenderer.Save(report, workbook);
            using ZipArchive archive = ZipFile.OpenRead(workbook);
            XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XDocument[] tables = archive.Entries
                .Where(static entry => entry.FullName.StartsWith("xl/tables/table", StringComparison.Ordinal))
                .Select(static entry => {
                    using Stream stream = entry.Open();
                    return XDocument.Load(stream);
                }).ToArray();
            Assert.Contains(tables, table => TableColumns(table, spreadsheet).Contains("Object Affected"));
            Assert.Contains(tables, table => TableColumns(table, spreadsheet).Contains("Failure Reason"));
            Assert.Contains(tables, table => TableColumns(table, spreadsheet).Contains("Event ID"));
            Assert.DoesNotContain(tables.Where(table => TableColumns(table, spreadsheet).Contains("Object Affected")),
                table => TableColumns(table, spreadsheet).Contains("Event ID"));
        } finally {
            File.Delete(workbook);
        }
    }

    [Fact]
    public async Task CompositeEmailReservesDigestRowsForEveryPopulatedType() {
        EventObject successfulSource = CreateSecuritySource(4624);
        EventObject failedSource = CreateSecuritySource(4625);
        object[] events = Enumerable.Repeat<object>(new ADUserLogon(successfulSource), 30)
            .Concat(new object[] { new ADUserLogonFailed(failedSource) })
            .ToArray();
        EventReport report = EventReportEngine.Create(events, "Authentication activity");

        EventEmailPackage email = await EventReportEmailRenderer.RenderAsync(report, maximumRows: 25);

        Assert.Contains("AD User Logon", email.Html, StringComparison.Ordinal);
        Assert.Contains("AD User Logon Failed", email.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TypedCollectionFieldsRenderAsReadableValues() {
        EventObject source = CreateSecuritySource(4672);
        source.Data["SubjectDomainName"] = "EVOTEC";
        source.Data["SubjectUserName"] = "alice";
        source.Data["PrivilegeList"] = "SeSecurityPrivilege\r\n\tSeBackupPrivilege";
        EventReport report = EventReportEngine.Create(new object[] {
            new ADUserPrivilegeUse(source)
        }, "Privilege use");

        string html = EventReportHtmlRenderer.Render(report);
        EventEmailPackage email = await EventReportEmailRenderer.RenderAsync(report);

        Assert.Contains("SeSecurityPrivilege", html, StringComparison.Ordinal);
        Assert.Contains("SeBackupPrivilege", html, StringComparison.Ordinal);
        Assert.Contains("SeSecurityPrivilege", email.Html, StringComparison.Ordinal);
        Assert.Contains("SeBackupPrivilege", email.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Collections.Generic.List", html, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Collections.Generic.List", email.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void WideTypedDefinitionKeepsItsOwnColumnsWithoutFallingBackToGenericDetails() {
        EventDefinition definition = new() {
            Name = "WideProviderEvent",
            Sources = CreateDefinition().Sources,
            Fields = Enumerable.Range(1, 13)
                .Select(index => new EventDefinitionField {
                    Name = $"Field{index}",
                    Source = EventFieldSource.Data,
                    SourceName = $"Value{index}"
                })
                .ToArray()
        };
        var source = new EventObject(new SyntheticEventRecord(), "WEC01", EventReadMode.StructuredDataAndMessage) {
            ContainerLog = "ForwardedEvents",
            GatheredLogName = "ForwardedEvents"
        };
        for (int index = 1; index <= 13; index++) {
            source.Data[$"Value{index}"] = $"value{index}";
        }
        EventReport report = EventReportEngine.Create(new object[] {
            EventDefinitionEngine.CreateRecord(definition, source)
        }, "Wide provider event");

        string html = EventReportHtmlRenderer.Render(report);
        Assert.Contains("Field13", html, StringComparison.Ordinal);
        Assert.Contains("value13", html, StringComparison.Ordinal);

        string workbook = Path.Combine(Path.GetTempPath(), $"evx-wide-report-{Guid.NewGuid():N}.xlsx");
        try {
            EventReportExcelRenderer.Save(report, workbook);
            using ZipArchive archive = ZipFile.OpenRead(workbook);
            XDocument eventTable = archive.Entries
                .Where(static entry => entry.FullName.StartsWith("xl/tables/table", StringComparison.Ordinal))
                .Select(static entry => {
                    using Stream stream = entry.Open();
                    return XDocument.Load(stream);
                })
                .Single(document => TableColumns(document,
                    "http://schemas.openxmlformats.org/spreadsheetml/2006/main").Contains("Field13"));
            XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            string[] columns = eventTable.Descendants(spreadsheet + "tableColumn")
                .Select(static column => column.Attribute("name")?.Value ?? string.Empty)
                .ToArray();

            Assert.Equal(13, columns.Length);
            Assert.Contains("Field13", columns);
            Assert.DoesNotContain("Details", columns);
        } finally {
            File.Delete(workbook);
        }
    }

    [Fact]
    public void DefinitionRejectsDuplicateFieldsAndInvalidEventIds() {
        EventDefinition definition = CreateDefinition();
        definition.Sources = new[] { new EventDefinitionSource { LogName = "Security", EventIds = new[] { 0 } } };
        definition.Fields = new[] {
            new EventDefinitionField { Name = "User", Source = EventFieldSource.Data, SourceName = "TargetUserName" },
            new EventDefinitionField { Name = "user", Source = EventFieldSource.Message }
        };
        InvalidDataException exception = Assert.Throws<InvalidDataException>(definition.Validate);
        Assert.Contains("EventIds", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DefinitionSnapshotFreezesMutableSourcesFieldsAndRecordIds() {
        EventDefinition definition = CreateDefinition();
        var query = new EventDefinitionQuery(definition) {
            RecordIds = new long[] { 41, 42 },
            MachineNames = new string?[] { " WEC01 " },
            MaxCandidates = 25,
            ReadMode = EventReadMode.Full,
            BufferCapacity = 32,
            RemoteConnectionTimeoutMilliseconds = 1500
        };

        EventDefinitionQuery snapshot = EventDefinitionEngine.CreateSnapshot(query);
        definition.Sources = new[] { new EventDefinitionSource { LogName = "System", EventIds = new[] { 41 } } };
        definition.Fields = Array.Empty<EventDefinitionField>();
        query.RecordIds = new long[] { 99 };

        Assert.Equal("Security", snapshot.Definition.Sources[0].LogName);
        Assert.Equal(new[] { 4625 }, snapshot.Definition.Sources[0].EventIds);
        Assert.Equal(new long[] { 41, 42 }, snapshot.RecordIds);
        Assert.Equal("WEC01", snapshot.MachineNames![0]);
        Assert.Equal(2, snapshot.Definition.Fields.Count);
        Assert.Equal(25, snapshot.MaxCandidates);
        Assert.Equal(EventReadMode.Full, snapshot.ReadMode);
        Assert.Equal(32, snapshot.BufferCapacity);
        Assert.Equal(1500, snapshot.RemoteConnectionTimeoutMilliseconds);
    }

    [Fact]
    public void CompilerNormalizesDuplicatesAndRejectsInvalidSourceContracts() {
        string xpath = EventDefinitionCompiler.BuildSourceXPath(
            "Security", new[] { 4625, 4625 }, new[] { "Provider", "provider" });

        Assert.Equal(1, xpath.Split("EventID=4625", StringSplitOptions.None).Length - 1);
        Assert.Equal(1, xpath.Split("@Name='Provider'", StringSplitOptions.None).Length - 1);
        Assert.Throws<ArgumentException>(() =>
            EventDefinitionCompiler.BuildSourceXPath("Security", Array.Empty<int>()));
        Assert.Throws<ArgumentException>(() =>
            EventDefinitionCompiler.BuildSourceXPath("Security", new[] { 1 }, new[] { " " }));
    }

    [Fact]
    public void CreateRowProducesAcyclicNormalizedWatcherPayload() {
        var source = new EventObject(new SyntheticEventRecord(), "WEC01", EventReadMode.StructuredDataAndMessage) {
            ContainerLog = "ForwardedEvents",
            GatheredLogName = "ForwardedEvents"
        };
        source.Data["TargetUserName"] = "alice";
        CustomEventRecord custom = EventDefinitionEngine.CreateRecord(CreateDefinition(), source);

        EventReportRow row = EventReportEngine.CreateRow(custom);

        Assert.Equal("FailedLogonCustom", row.Type);
        Assert.Equal("alice", row.Values["User"]);
        Assert.DoesNotContain(row.GetType().GetProperties(), static property =>
            property.PropertyType == typeof(EventObject));
    }

    [Fact]
    public async Task OfflineReportUsesTheSharedBatchEngineAndTracksFileCoverage() {
        string fixture = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Tests", "Logs", "NamedFilterExamples.evtx"));
        EventReportRequest request = EventReportRequest.ForFiles(fixture);
        request.MaxEvents = 3;
        request.Oldest = true;

        EventReport report = await EventReportEngine.QueryAsync(request);

        Assert.Equal(3, report.Rows.Count);
        EventReportSection section = Assert.Single(report.Sections);
        Assert.Equal(EventReportSectionKind.Generic, section.Kind);
        Assert.Contains(section.Columns, static column => column.Name == nameof(EventReportRow.EventId));
        Assert.Contains(section.Columns, static column => column.Name == nameof(EventReportRow.Provider));
        Assert.Single(report.Coverage);
        Assert.Equal("Offline", report.Coverage[0].MachineName);
        Assert.Equal(fixture, report.Coverage[0].LogName);
    }

    [Fact]
    public async Task CustomDefinitionOwnsSemanticsWhenReadingOfflineFiles() {
        string fixture = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Tests", "Logs", "NamedFilterExamples.evtx"));
        EventDefinition definition = new() {
            Name = "ServiceStartTypeChange",
            Sources = new[] {
                new EventDefinitionSource {
                    LogName = "System",
                    EventIds = new[] { 7040 },
                    ProviderNames = new[] { "Service Control Manager" }
                }
            },
            Fields = new[] {
                new EventDefinitionField { Name = "ServiceName", Source = EventFieldSource.Data, SourceName = "param1" }
            }
        };
        EventReportRequest request = EventReportRequest.ForDefinition(definition);
        request.Paths = new[] { fixture };
        request.MaxEvents = 2;

        EventReport report = await EventReportEngine.QueryAsync(request);

        Assert.Equal(2, report.Rows.Count);
        Assert.All(report.Rows, static row => Assert.Equal("ServiceStartTypeChange", row.Type));
        Assert.Single(report.Coverage);
        Assert.Equal("Offline", report.Coverage[0].MachineName);
    }

    [Fact]
    public async Task ReportRequestRejectsConcurrencyOutsideTheCoreEngineContract() {
        EventReportRequest request = EventReportRequest.ForLog("System");
        request.MaxConcurrency = EventLogLimits.MaximumConcurrency + 1;

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => EventReportEngine.QueryAsync(request));

        Assert.Contains("MaxConcurrency must be between", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CustomDefinitionAppliesResultLimitAfterProjectionPredicate() {
        string fixture = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Tests", "Logs", "NamedFilterExamples.evtx"));
        EventDefinition definition = new() {
            Name = "ServiceStartTypeChange",
            Sources = new[] {
                new EventDefinitionSource {
                    LogName = "System",
                    EventIds = new[] { 7040 },
                    ProviderNames = new[] { "Service Control Manager" }
                }
            }
        };
        var all = new List<CustomEventRecord>();
        await foreach (CustomEventRecord record in EventDefinitionEngine.ReadAsync(
                           new EventDefinitionQuery(definition) { Paths = new[] { fixture } })) {
            all.Add(record);
        }
        Assert.True(all.Count >= 2);
        long selectedRecordId = all[^1].SourceEvent.RecordId!.Value;
        var query = new EventDefinitionQuery(definition) {
            Paths = new[] { fixture },
            MaxEvents = 1,
            ResultPredicate = record => record.SourceEvent.RecordId == selectedRecordId
        };
        var actual = new List<CustomEventRecord>();

        await foreach (CustomEventRecord record in EventDefinitionEngine.ReadAsync(query)) {
            actual.Add(record);
        }

        Assert.Single(actual);
        Assert.Equal(selectedRecordId, actual[0].SourceEvent.RecordId);
    }

    [Fact]
    public async Task CustomDefinitionReportsCandidateTruncationOnlyWhenAnotherCandidateExists() {
        string fixture = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Tests", "Logs", "NamedFilterExamples.evtx"));
        EventDefinition definition = new() {
            Name = "ServiceStartTypeChange",
            Sources = new[] {
                new EventDefinitionSource { LogName = "System", EventIds = new[] { 7040 } }
            }
        };
        var query = new EventDefinitionQuery(definition) {
            Paths = new[] { fixture },
            MaxCandidates = 1,
            ResultPredicate = static _ => false
        };
        var info = new EventDefinitionQueryExecutionInfo();

        await foreach (CustomEventRecord _ in EventDefinitionEngine.ReadAsync(query, info)) {
        }

        Assert.Equal(1, info.EventsScanned);
        Assert.Equal(0, info.EventsEmitted);
        Assert.True(info.ScanLimitReached);
    }

    [Fact]
    public void OfflineDefinitionSnapshotFreezesPathsAndRejectsRemoteMixing() {
        string[] paths = { "one.evtx", "two.evtx" };
        var query = new EventDefinitionQuery(CreateDefinition()) { Paths = paths };

        EventDefinitionQuery snapshot = EventDefinitionEngine.CreateSnapshot(query);
        paths[0] = "changed.evtx";

        Assert.Equal(new[] { "one.evtx", "two.evtx" }, snapshot.Paths);
        query.MachineNames = new[] { "server" };
        Assert.Throws<ArgumentException>(() => EventDefinitionEngine.CreateSnapshot(query));
    }

    private static EventDefinition CreateDefinition() => new() {
        Name = "FailedLogonCustom",
        DisplayName = "Custom failed logons",
        Sources = new[] {
            new EventDefinitionSource {
                LogName = "Security",
                EventIds = new[] { 4625 },
                ProviderNames = new[] { "Microsoft-Windows-Security-Auditing" }
            }
        },
        Fields = new[] {
            new EventDefinitionField { Name = "User", Source = EventFieldSource.Data, SourceName = "TargetUserName" },
            new EventDefinitionField { Name = "Computer", Source = EventFieldSource.Metadata, SourceName = "SourceComputer" }
        }
    };

    private static EventObject CreateSecuritySource(int eventId) => new(
        new SyntheticEventRecord(eventId), "WEC01", EventReadMode.StructuredDataAndMessage) {
            ContainerLog = "ForwardedEvents",
            GatheredLogName = "ForwardedEvents"
        };

    private static string[] TableColumns(XDocument table, XNamespace spreadsheet) => table
        .Descendants(spreadsheet + "tableColumn")
        .Select(static column => column.Attribute("name")?.Value ?? string.Empty)
        .ToArray();

    private sealed class SyntheticEventRecord : EventRecord {
        private readonly int _eventId;

        internal SyntheticEventRecord(int eventId = 4625) {
            _eventId = eventId;
        }

        public override string ProviderName => "Microsoft-Windows-Security-Auditing";
        public override string LogName => "Security";
        public override string MachineName => "source.ad.evotec.xyz";
        public override int Id => _eventId;
        public override byte? Level => 0;
        public override int? Task => 12544;
        public override long? Keywords => 0;
        public override IEnumerable<string> KeywordsDisplayNames => Array.Empty<string>();
        public override short? Opcode => 0;
        public override string OpcodeDisplayName => string.Empty;
        public override string TaskDisplayName => string.Empty;
        public override Guid? ProviderId => null;
        public override Guid? ActivityId => null;
        public override Guid? RelatedActivityId => null;
        public override int? ProcessId => 1;
        public override int? ThreadId => 1;
        public override string LevelDisplayName => "Information";
        public override IList<EventProperty> Properties => Array.Empty<EventProperty>();
        public override DateTime? TimeCreated => new DateTime(2026, 8, 16, 10, 0, 0, DateTimeKind.Utc);
        public override int? Qualifiers => null;
        public override long? RecordId => 42;
        public override byte? Version => 0;
        public override SecurityIdentifier UserId => null!;
        public override EventBookmark Bookmark => null!;
        public override string FormatDescription() => "An account failed to log on.";
        public override string FormatDescription(IEnumerable<object> values) => FormatDescription();
        public override string ToXml() => "<Event><EventData><Data Name=\"TargetUserName\">alice</Data></EventData></Event>";
    }
}

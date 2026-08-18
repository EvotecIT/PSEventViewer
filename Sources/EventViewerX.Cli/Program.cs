using System.Globalization;
using System.Text;
using System.Text.Json;
using EventViewerX.Providers;
using EventViewerX.Reporting;

namespace EventViewerX.Cli;

internal static partial class Program {
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private static async Task<int> Main(string[] args) {
        try {
            var options = new CliArguments(args);
            ValidateOptions(options);
            return options.Command switch {
                "query" => await QueryAsync(options).ConfigureAwait(false),
                "report" => await ReportAsync(options).ConfigureAwait(false),
                "watch" => await WatchAsync(options).ConfigureAwait(false),
                "collector" => Collector(options),
                "provider" => Provider(options),
                "types" => ListTypes(),
                "help" or "--help" or "-h" => Help(),
                _ => throw new ArgumentException($"Unknown command '{options.Command}'.")
            };
        } catch (OperationCanceledException) {
            Console.Error.WriteLine("Canceled.");
            return 130;
        } catch (Exception exception) {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static async Task<int> QueryAsync(CliArguments options) {
        EventReport report = await EventReportEngine.QueryAsync(CreateRequest(options)).ConfigureAwait(false);
        foreach (EventReportRow row in report.Rows) {
            Console.WriteLine(JsonSerializer.Serialize(row.ToDictionary(), JsonOptions));
        }
        return 0;
    }

    private static async Task<int> ReportAsync(CliArguments options) {
        EventReport report = await EventReportEngine.QueryAsync(CreateRequest(options)).ConfigureAwait(false);
        bool written = false;
        EventEmailPackage? emailPackage = null;
        if (options.Get("html") is string html) {
            Console.WriteLine(EventReportHtmlRenderer.Save(report, html));
            written = true;
        }
        if (options.Get("excel") is string excel) {
            Console.WriteLine(EventReportExcelRenderer.Save(report, excel));
            written = true;
        }
        if (options.Get("email-html") is string emailHtml) {
            emailPackage = await EventReportEmailRenderer.RenderAsync(report, options.GetInt("email-rows", 25)).ConfigureAwait(false);
            string fullPath = Path.GetFullPath(emailHtml);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, emailPackage.Html, new UTF8Encoding(false)).ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.ChangeExtension(fullPath, ".txt"), emailPackage.PlainText, new UTF8Encoding(false)).ConfigureAwait(false);
            Console.WriteLine(fullPath);
            written = true;
        }
        if (options.Get("mail-profile") is string mailProfile) {
            emailPackage ??= await EventReportEmailRenderer.RenderAsync(report, options.GetInt("email-rows", 25)).ConfigureAwait(false);
            SmtpNotificationProfile profile = SmtpNotificationProfile.Load(mailProfile);
            Mailozaurr.SmtpResult result = await profile.SendAsync(emailPackage, report.Title).ConfigureAwait(false);
            Console.WriteLine(JsonSerializer.Serialize(new {
                Delivered = result.Status,
                profile.DryRun,
                result.Server,
                result.Port,
                result.MessageId,
                result.TimeToExecute
            }, JsonOptions));
            written = true;
        }
        if (!written) {
            throw new ArgumentException("report requires --html, --excel, --email-html, or --mail-profile.");
        }
        return 0;
    }

    private static EventReportRequest CreateRequest(CliArguments options) {
        bool hasDefinition = options.Get("definition") != null;
        bool hasTypes = options.GetMany("type").Length > 0;
        bool hasPaths = options.GetMany("path").Length > 0;
        bool hasLog = options.Get("log") != null;
        int logicalDefinitions = new[] { hasDefinition, hasTypes, hasLog }.Count(static value => value);
        if (logicalDefinitions > 1 || logicalDefinitions == 0 && !hasPaths || hasLog && hasPaths) {
            throw new ArgumentException("query and report require one of --type, --definition, --log, or standalone --path; --path may accompany --type or --definition.");
        }
        EventReportRequest request;
        if (options.Get("definition") is string path) {
            request = EventReportRequest.ForDefinition(EventDefinition.Load(path));
        } else if (options.GetMany("type").Length > 0) {
            request = EventReportRequest.ForTypes(ParseTypes(options.GetMany("type")));
        } else if (options.GetMany("path").Length > 0) {
            request = EventReportRequest.ForFiles(options.GetMany("path"));
        } else {
            request = EventReportRequest.ForLog(options.Require("log"));
        }
        if (hasPaths && (hasTypes || hasDefinition)) {
            request.Paths = options.GetMany("path");
        }
        request.EventIds = ParseInts(options.GetMany("event-id"));
        request.RecordIds = ParseLongs(options.GetMany("record-id"));
        request.MachineNames = NullWhenEmpty(options.GetMany("machine"));
        request.Collectors = NullWhenEmpty(options.GetMany("collector"));
        request.StartTime = ParseDate(options.Get("start"));
        request.EndTime = ParseDate(options.Get("end"));
        if (options.Get("since") is string since) {
            request.StartTime = DateTime.Now.Subtract(TimeSpan.Parse(since, CultureInfo.InvariantCulture));
        }
        request.MaxEvents = options.GetLong("max");
        request.MaxCandidates = options.GetLong("max-candidates");
        request.MaxConcurrency = options.GetInt("concurrency", 8);
        request.Oldest = options.Has("oldest");
        request.ResolveDns = options.Has("resolve-dns");
        request.Title = options.Get("title");
        return request;
    }

    private static int Collector(CliArguments options) {
        if (options.Subcommand == "remove") {
            return WriteJson(CollectorSubscriptionManager.RemoveCollectorSubscription(options.Require("name")));
        }
        if (options.Subcommand == "readiness") {
            return WriteJson(CollectorSubscriptionManager.GetCollectorReadiness());
        }
        if (options.Subcommand == "runtime") {
            return WriteJson(CollectorSubscriptionManager.GetCollectorSubscriptionRuntimeStatus(options.Require("name")));
        }
        if (options.Subcommand == "initialize") {
            return WriteJson(CollectorSubscriptionManager.InitializeCollector(!options.Has("skip-winrm")));
        }
        if (options.Subcommand != "create") {
            throw new ArgumentException("collector supports create, remove, readiness, runtime, and initialize.");
        }
        EventType[] types = ParseTypes(options.GetMany("type"));
        if (types.Length == 0) {
            throw new ArgumentException("--type is required.");
        }
        string[] computers = options.GetMany("source");
        bool sourceInitiated = options.Has("source-initiated");
        if (!sourceInitiated && computers.Length == 0) {
            throw new ArgumentException("--source is required.");
        }
        if (sourceInitiated && computers.Length > 0) {
            throw new ArgumentException("--source cannot be used with --source-initiated; authorize source SIDs with --allowed-source-sddl.");
        }
        CollectorSubscriptionDeliveryMode deliveryMode = options.Get("delivery") is string delivery
            ? Enum.TryParse(delivery, true, out CollectorSubscriptionDeliveryMode parsedDelivery)
                ? parsedDelivery
                : throw new ArgumentException("--delivery must be Pull or Push.")
            : sourceInitiated ? CollectorSubscriptionDeliveryMode.Push : CollectorSubscriptionDeliveryMode.Pull;
        var definition = new CollectorSubscriptionDefinition {
            SubscriptionId = options.Require("name"),
            Description = options.Get("description") ?? $"EventViewerX {string.Join(", ", types.Select(static type => type.ToString()))}",
            Enabled = !options.Has("disabled"),
            SubscriptionType = sourceInitiated
                ? CollectorSubscriptionType.SourceInitiated
                : CollectorSubscriptionType.CollectorInitiated,
            QueryXml = EventDefinitionCompiler.BuildQueryXml(types),
            Sources = computers.Select(static computer => new CollectorSubscriptionSource(computer)).ToArray(),
            ReadExistingEvents = options.Has("read-existing"),
            DeliveryMode = deliveryMode,
            CollectorHostName = options.Get("collector-host"),
            AllowedSourceDomainComputersSddl = options.Get("allowed-source-sddl") ?? "O:NSG:NSD:(A;;GA;;;DC)(A;;GA;;;NS)",
            SourceRefreshIntervalSeconds = options.GetInt("source-refresh", 60)
        };
        definition.Validate();
        if (options.Get("output") is string output) {
            Console.WriteLine(CollectorSubscriptionManager.WriteCollectorSubscriptionDefinition(definition, output, options.Has("force")).FullName);
        }
        if (options.Has("apply")) {
            Console.WriteLine(JsonSerializer.Serialize(CollectorSubscriptionManager.ApplyCollectorSubscription(definition), JsonOptions));
        }
        if (!options.Has("apply") && options.Get("output") == null) {
            Console.WriteLine(definition.ToXml());
        }
        return 0;
    }

    private static int Provider(CliArguments options) {
        return options.Subcommand switch {
            "build" => ProviderBuild(options),
            "install" => WriteJson(EventProviderPackageManager.Install(options.Require("package"))),
            "uninstall" => WriteJson(EventProviderPackageManager.Uninstall(options.Require("name"), options.Has("remove-files"))),
            _ => throw new ArgumentException("provider supports build, install, and uninstall.")
        };
    }

    private static int ProviderBuild(CliArguments options) {
        EventProviderDefinition definition = EventProviderDefinitionJson.Load(options.Require("definition"));
        EventProviderPackageBuildResult result = EventProviderPackageBuilder.Build(definition, options.Require("output"),
            new EventProviderPackageBuildOptions { Overwrite = options.Has("force"), BaselinePath = options.Get("baseline") ?? string.Empty });
        return WriteJson(result);
    }

    private static int ListTypes() {
        foreach (EventTypeDefinition definition in EventTypeCatalog.GetDefinitions()) {
            Console.WriteLine(JsonSerializer.Serialize(new {
                definition.Name,
                definition.DisplayName,
                definition.Description,
                definition.Category,
                definition.IsComposite,
                Sources = definition.Sources.Select(static source => new { source.LogName, source.EventIds })
            }, JsonOptions));
        }
        return 0;
    }

    private static int WriteJson<T>(T value) {
        Console.WriteLine(JsonSerializer.Serialize(value, JsonOptions));
        return 0;
    }

    private static EventType[] ParseTypes(IEnumerable<string> values) => values.Select(value =>
        Enum.TryParse(value, ignoreCase: true, out EventType parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ArgumentException($"Unknown event type '{value}'. Use 'evx types' to list definitions."))
        .Distinct().ToArray();
    private static int[]? ParseInts(string[] values) => values.Length == 0 ? null : values.Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
    private static long[]? ParseLongs(string[] values) => values.Length == 0 ? null : values.Select(value => long.Parse(value, CultureInfo.InvariantCulture)).ToArray();
    private static string[]? NullWhenEmpty(string[] values) => values.Length == 0 ? null : values;
    private static DateTime? ParseDate(string? value) => value == null ? null : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);

    private static void ValidateOptions(CliArguments options) {
        if (options.Subcommand.Length > 0 && options.Command is not ("collector" or "provider")) {
            throw new ArgumentException(
                $"Unexpected argument '{options.Subcommand}'. The {options.Command} command does not accept a subcommand.");
        }

        switch (options.Command) {
            case "query":
                options.ValidateAllowed(
                    "type", "definition", "log", "path", "event-id", "record-id",
                    "machine", "collector", "start", "end", "since", "max",
                    "max-candidates", "concurrency", "oldest", "resolve-dns", "title");
                break;
            case "report":
                options.ValidateAllowed(
                    "type", "definition", "log", "path", "event-id", "record-id",
                    "machine", "collector", "start", "end", "since", "max",
                    "max-candidates", "concurrency", "oldest", "resolve-dns", "title",
                    "html", "excel", "email-html", "mail-profile", "email-rows");
                break;
            case "watch":
                options.ValidateAllowed(
                    "type", "definition", "machine", "collector", "jsonl", "outbox",
                    "mail-profile", "interval", "stop-after", "timeout", "ready-file",
                    "summary-file", "title");
                break;
            case "collector" when options.Subcommand == "create":
                options.ValidateAllowed(
                    "name", "source", "type", "description", "disabled", "read-existing",
                    "output", "force", "apply", "source-initiated", "allowed-source-sddl",
                    "delivery", "collector-host", "source-refresh");
                break;
            case "collector" when options.Subcommand == "remove":
            case "collector" when options.Subcommand == "runtime":
                options.ValidateAllowed("name");
                break;
            case "collector" when options.Subcommand == "readiness":
                options.ValidateAllowed();
                break;
            case "collector" when options.Subcommand == "initialize":
                options.ValidateAllowed("skip-winrm");
                break;
            case "provider" when options.Subcommand == "build":
                options.ValidateAllowed("definition", "output", "force", "baseline");
                break;
            case "provider" when options.Subcommand == "install":
                options.ValidateAllowed("package");
                break;
            case "provider" when options.Subcommand == "uninstall":
                options.ValidateAllowed("name", "remove-files");
                break;
            case "types":
            case "help":
            case "--help":
            case "-h":
                options.ValidateAllowed();
                break;
        }
    }

    private static int Help() {
        Console.WriteLine("EventViewerX 4.0\n\n" +
            "  evx types\n" +
            "  evx query  (--type TYPE[,TYPE] | --definition FILE | --log LOG | --path FILE[,FILE]) [--path FILE[,FILE] with type/definition] [--event-id ID] [--record-id ID] [--machine HOST | --collector WEC] [--since 01:00:00] [--max N]\n" +
            "  evx report (--type TYPE[,TYPE] | --definition FILE | --log LOG | --path FILE[,FILE]) [--path FILE[,FILE] with type/definition] (--html FILE | --excel FILE | --email-html FILE | --mail-profile FILE)\n" +
            "  evx watch  (--type TYPE[,TYPE] | --definition FILE) [--machine HOST | --collector WEC] [--jsonl FILE] [--outbox DIR | --mail-profile FILE] [--interval 00:05:00] [--stop-after N] [--timeout 01:00:00] [--ready-file FILE] [--summary-file FILE]\n" +
            "  evx collector create --name NAME --type TYPE[,TYPE] (--source HOST[,HOST] | --source-initiated --collector-host WEC) [--allowed-source-sddl SDDL] [--output FILE] [--apply]\n" +
            "  evx collector readiness\n" +
            "  evx collector runtime --name NAME\n" +
            "  evx collector initialize [--skip-winrm]\n" +
            "  evx collector remove --name NAME\n" +
            "  evx provider build --definition FILE --output FILE.evxprovider\n" +
            "  evx provider install --package FILE.evxprovider\n" +
            "  evx provider uninstall --name PROVIDER [--remove-files]");
        return 0;
    }
}

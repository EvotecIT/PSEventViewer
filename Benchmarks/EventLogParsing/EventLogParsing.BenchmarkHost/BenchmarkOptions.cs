namespace EventLogParsing.BenchmarkHost;

internal sealed class BenchmarkOptions {
    public required string Engine { get; init; }

    public required string Path { get; init; }

    public required EventViewerX.EventReadMode ReadMode { get; init; }

    public required string ResultPath { get; init; }

    public string? OutputPath { get; init; }

    public int MaxEvents { get; init; }

    public static BenchmarkOptions Parse(string[] args) {
        if (args is null) {
            throw new ArgumentNullException(nameof(args));
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++) {
            string argument = args[i];
            if (!argument.StartsWith("--", StringComparison.Ordinal)) {
                throw new ArgumentException($"Unexpected argument '{argument}'.");
            }
            if (i + 1 >= args.Length) {
                throw new ArgumentException($"Argument '{argument}' requires a value.");
            }

            values[argument[2..]] = args[++i];
        }

        string engine = GetRequired(values, "engine");
        if (!string.Equals(engine, "dotnet", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(engine, "propertyselector", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(engine, "eventviewerx", StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("--engine must be 'dotnet', 'propertyselector', or 'eventviewerx'.");
        }

        string path = System.IO.Path.GetFullPath(GetRequired(values, "path"));
        if (!File.Exists(path)) {
            throw new FileNotFoundException($"The EVTX fixture '{path}' does not exist.", path);
        }

        if (!Enum.TryParse(GetRequired(values, "mode"), ignoreCase: true, out EventViewerX.EventReadMode readMode)) {
            throw new ArgumentException("--mode must be Metadata, Message, StructuredData, or Full.");
        }

        int maxEvents = 0;
        if (values.TryGetValue("max-events", out string? maxText) &&
            (!int.TryParse(maxText, out maxEvents) || maxEvents < 0)) {
            throw new ArgumentException("--max-events must be a non-negative integer.");
        }

        return new BenchmarkOptions {
            Engine = engine,
            Path = path,
            ReadMode = readMode,
            ResultPath = System.IO.Path.GetFullPath(GetRequired(values, "result")),
            OutputPath = values.TryGetValue("output-path", out string? outputPath)
                ? System.IO.Path.GetFullPath(outputPath)
                : null,
            MaxEvents = maxEvents
        };
    }

    private static string GetRequired(IReadOnlyDictionary<string, string> values, string name) {
        if (!values.TryGetValue(name, out string? value) || string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException($"--{name} is required.");
        }

        return value.Trim();
    }
}

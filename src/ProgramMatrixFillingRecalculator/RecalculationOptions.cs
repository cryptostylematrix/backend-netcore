namespace ProgramMatrixFillingRecalculator;

internal sealed record RecalculationOptions(
    string ConnectionString,
    string? MarketingAddr,
    bool ApplyChanges)
{
    public static RecalculationOptions Parse(string[] args)
    {
        var arguments = ParseArguments(args);
        if (arguments.ContainsKey("help"))
            throw new OptionsException(Usage, showUsageOnly: true);

        var connectionString = Get(
            arguments,
            "connection-string",
            "PROGRAM_MATRIX_FILLING_CONNECTION_STRING",
            "ConnectionStrings__Programs");
        var marketingAddr = Get(
            arguments,
            "marketing-addr",
            "PROGRAM_MATRIX_FILLING_MARKETING_ADDR");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw Error("The Programs database connection string is required.");

        return new RecalculationOptions(
            connectionString,
            string.IsNullOrWhiteSpace(marketingAddr) ? null : marketingAddr.Trim(),
            arguments.ContainsKey("apply"));
    }

    public const string Usage = """
        Recalculates persisted matrix filling for Referral Programs.

        The command is a dry run unless --apply is supplied. Run --apply while the
        API task processor and other writers for the Programs database are stopped.

        Usage:
          dotnet run --project src/ProgramMatrixFillingRecalculator -- [options]

        Required options (or corresponding environment variables):
          --connection-string VALUE    PROGRAM_MATRIX_FILLING_CONNECTION_STRING
                                       or ConnectionStrings__Programs
        Optional:
          --marketing-addr VALUE       Process only this marketing address; uses
                                       PROGRAM_MATRIX_FILLING_MARKETING_ADDR.
                                       Omit it to process every Referral Program.
          --env-file VALUE             Load settings from this .env file
          --apply                      Update PostgreSQL; otherwise dry-run
          --help                       Show this help
        """;

    private static Dictionary<string, string?> ParseArguments(string[] args)
    {
        var valueArguments = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "marketing-addr",
            "connection-string",
            "env-file"
        };
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
                throw Error($"Unexpected argument '{argument}'.");

            var name = argument[2..];
            if (name is "apply" or "help")
            {
                if (!result.TryAdd(name, null))
                    throw Error($"Argument '--{name}' was provided more than once.");
                continue;
            }

            if (!valueArguments.Contains(name))
                throw Error($"Unknown argument '--{name}'.");

            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                throw Error($"Argument '--{name}' requires a value.");

            if (!result.TryAdd(name, args[++index]))
                throw Error($"Argument '--{name}' was provided more than once.");
        }

        return result;
    }

    private static string? Get(
        IReadOnlyDictionary<string, string?> arguments,
        string argumentName,
        params string[] environmentNames)
    {
        if (arguments.TryGetValue(argumentName, out var argumentValue))
            return argumentValue;

        return environmentNames
            .Select(Environment.GetEnvironmentVariable)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static OptionsException Error(string message) =>
        new($"{message}{Environment.NewLine}{Environment.NewLine}{Usage}");
}

internal sealed class OptionsException(string message, bool showUsageOnly = false)
    : Exception(message)
{
    public bool ShowUsageOnly { get; } = showUsageOnly;
}

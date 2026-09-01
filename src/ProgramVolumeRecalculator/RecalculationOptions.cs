namespace ProgramVolumeRecalculator;

internal enum VolumeType
{
    Personal,
    Referral,
    Group
}

internal sealed record RecalculationOptions(
    string ConnectionString,
    string MarketingAddr,
    byte StructureNumber,
    VolumeType Type,
    bool ApplyChanges)
{
    public static RecalculationOptions Parse(string[] args)
    {
        var arguments = ParseArguments(args);
        if (arguments.ContainsKey("help"))
            throw new OptionsException(Usage, showUsageOnly: true);

        var connectionString = Get(arguments, "connection-string",
            "PROGRAM_VOLUME_CONNECTION_STRING", "ConnectionStrings__Programs");
        var marketingAddr = Get(arguments, "marketing-addr", "PROGRAM_VOLUME_MARKETING_ADDR");
        var structureValue = Get(arguments, "structure-number", "PROGRAM_VOLUME_STRUCTURE_NUMBER");
        var typeValue = Get(arguments, "type", "PROGRAM_VOLUME_TYPE");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw Error("The Programs database connection string is required.");
        if (string.IsNullOrWhiteSpace(marketingAddr))
            throw Error("Marketing address is required.");
        if (!byte.TryParse(structureValue, out var structureNumber))
            throw Error("Structure number must be between 0 and 255.");
        if (!Enum.TryParse<VolumeType>(typeValue, ignoreCase: true, out var type))
            throw Error("Volume type must be personal, referral, or group.");

        return new RecalculationOptions(
            connectionString,
            marketingAddr.Trim(),
            structureNumber,
            type,
            arguments.ContainsKey("apply"));
    }

    public const string Usage = """
        Recalculates one profile-volume type for one Referral Program structure.

        Places with a profile and activated_at IS NOT NULL are counted. The command
        is a dry run unless --apply is supplied. Group volume is accepted but is not
        implemented and is never changed.

        Usage:
          dotnet run --project src/ProgramVolumeRecalculator -- [options]

        Required options (or corresponding environment variables):
          --connection-string VALUE    PROGRAM_VOLUME_CONNECTION_STRING
                                       or ConnectionStrings__Programs
          --marketing-addr VALUE       PROGRAM_VOLUME_MARKETING_ADDR
          --structure-number VALUE     PROGRAM_VOLUME_STRUCTURE_NUMBER
          --type VALUE                 PROGRAM_VOLUME_TYPE; personal, referral, group
        Optional:
          --env-file VALUE             Load settings from this .env file
          --apply                      Update PostgreSQL; otherwise dry-run
          --help                       Show this help
        """;

    private static Dictionary<string, string?> ParseArguments(string[] args)
    {
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "connection-string", "marketing-addr", "structure-number", "type", "env-file"
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

            if (!values.Contains(name))
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

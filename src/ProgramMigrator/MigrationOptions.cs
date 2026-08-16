using System.Globalization;

namespace ProgramMigrator;

internal sealed record MigrationOptions(
    Uri ApiBaseUrl,
    string ConnectionString,
    string SourceConnectionString,
    string MarketingAddr,
    string SourceMarketingAddr,
    uint ProgramId,
    MigrationScope Scope,
    string? RootProfileAddr,
    string? RootProfileLogin,
    int MaxInvites,
    TimeSpan RequestDelay,
    bool ApplyChanges)
{
    public static MigrationOptions Parse(string[] args)
    {
        var arguments = ParseArguments(args);

        if (arguments.ContainsKey("help"))
            throw new OptionsException(Usage, showUsageOnly: true);

        var applyChanges = arguments.ContainsKey("apply");

        var apiBaseUrl = Get(arguments, "api-base-url", "PROGRAM_MIGRATOR_API_BASE_URL")
            ?? "http://localhost:5004";
        var connectionString = Get(
            arguments,
            "connection-string",
            "PROGRAM_MIGRATOR_CONNECTION_STRING",
            "ConnectionStrings__Programs");
        var sourceConnectionString = Get(
            arguments,
            "source-connection-string",
            "PROGRAM_MIGRATOR_SOURCE_CONNECTION_STRING",
            "ConnectionStrings__Matrix");
        var marketingAddr = Get(arguments, "marketing-addr", "PROGRAM_MIGRATOR_MARKETING_ADDR");
        var sourceMarketingAddr = Get(
            arguments,
            "source-marketing-addr",
            "PROGRAM_MIGRATOR_SOURCE_MARKETING_ADDR");
        var programIdText = Get(arguments, "program-id", "PROGRAM_MIGRATOR_PROGRAM_ID");
        var scopeText = Get(arguments, "scope", "PROGRAM_MIGRATOR_SCOPE");
        var rootProfileAddr = Get(arguments, "root-profile-addr", "PROGRAM_MIGRATOR_ROOT_PROFILE_ADDR");
        var rootProfileLogin = Get(arguments, "root-profile-login", "PROGRAM_MIGRATOR_ROOT_PROFILE_LOGIN");
        var maxInvitesText = Get(arguments, "max-invites", "PROGRAM_MIGRATOR_MAX_INVITES")
            ?? "100000";
        var requestDelayText = Get(
                arguments,
                "request-delay-ms",
                "PROGRAM_MIGRATOR_REQUEST_DELAY_MS")
            ?? "120";

        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri))
            throw Error("API base URL is invalid.");

        if (applyChanges && string.IsNullOrWhiteSpace(connectionString))
            throw Error("The Programs database connection string is required.");

        if (string.IsNullOrWhiteSpace(marketingAddr))
            throw Error("Marketing address is required.");

        if (!TryParseProgramId(programIdText, out var programId))
            throw Error("Program id must be an unsigned decimal or hexadecimal value.");

        if (!Enum.TryParse<MigrationScope>(scopeText, ignoreCase: true, out var scope))
            throw Error("Scope must be either 'invite' or 'structures'.");

        if (scope == MigrationScope.Invite
            && string.IsNullOrWhiteSpace(rootProfileAddr)
            && string.IsNullOrWhiteSpace(rootProfileLogin))
        {
            throw Error("Root profile address or root profile login is required.");
        }

        if (scope == MigrationScope.Structures
            && string.IsNullOrWhiteSpace(sourceConnectionString))
        {
            throw Error("The Matrix database connection string is required.");
        }

        if (!int.TryParse(maxInvitesText, out var maxInvites) || maxInvites <= 0)
            throw Error("Max invites must be a positive integer.");

        if (!int.TryParse(requestDelayText, out var requestDelayMs) || requestDelayMs < 0)
            throw Error("Request delay must be a non-negative number of milliseconds.");

        return new MigrationOptions(
            apiUri,
            connectionString ?? string.Empty,
            sourceConnectionString ?? string.Empty,
            marketingAddr.Trim(),
            string.IsNullOrWhiteSpace(sourceMarketingAddr)
                ? marketingAddr.Trim()
                : sourceMarketingAddr.Trim(),
            programId,
            scope,
            NullIfWhiteSpace(rootProfileAddr),
            NullIfWhiteSpace(rootProfileLogin)?.ToLowerInvariant(),
            maxInvites,
            TimeSpan.FromMilliseconds(requestDelayMs),
            applyChanges);
    }

    public const string Usage = """
        Imports a legacy Multi or Neo program into the ReferralProgram database.

        Use scope 'invite' to import structure 0 from Invite and Profile contracts.
        Use scope 'structures' to import structures 1+ and their locks from the
        legacy Matrix database. Run each scope separately.

        Usage:
          dotnet run --project src/ProgramMigrator -- [options]

        Required options (or corresponding environment variables):
          --marketing-addr VALUE       PROGRAM_MIGRATOR_MARKETING_ADDR
          --program-id VALUE           PROGRAM_MIGRATOR_PROGRAM_ID
          --scope VALUE                invite or structures
        Optional:
          --connection-string VALUE    Required with --apply; uses
                                       PROGRAM_MIGRATOR_CONNECTION_STRING or
                                       ConnectionStrings__Programs
          --root-profile-login VALUE   Resolve the root Profile NFT by login
          --root-profile-addr VALUE    Required for invite scope unless login is used
          --source-connection-string VALUE
                                       Required for structures scope; falls back to
                                       PROGRAM_MIGRATOR_SOURCE_CONNECTION_STRING
                                       or ConnectionStrings__Matrix
          --env-file VALUE             Load settings from this .env file
          --api-base-url VALUE         Default: http://localhost:5004
          --source-marketing-addr VALUE
                                       Source Neo marketing address; defaults
                                       to --marketing-addr (ignored for Multi)
          --max-invites VALUE          Default: 100000
          --request-delay-ms VALUE     Default: 120
          --apply                      Write to PostgreSQL; otherwise dry-run
          --help                       Show this help

        Program ids:
          Multi: 0x1ce8c484
          Neo:   0x435acabf
        """;

    private static Dictionary<string, string?> ParseArguments(string[] args)
    {
        var valueArguments = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "marketing-addr",
            "program-id",
            "scope",
            "root-profile-addr",
            "root-profile-login",
            "env-file",
            "connection-string",
            "source-connection-string",
            "source-marketing-addr",
            "api-base-url",
            "max-invites",
            "request-delay-ms"
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
                result[name] = null;
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

    private static bool TryParseProgramId(string? value, out uint programId)
    {
        programId = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(
                normalized[2..],
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture,
                out programId);
        }

        return uint.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out programId);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static OptionsException Error(string message) =>
        new($"{message}{Environment.NewLine}{Environment.NewLine}{Usage}");
}

internal sealed class OptionsException(string message, bool showUsageOnly = false)
    : Exception(message)
{
    public bool ShowUsageOnly { get; } = showUsageOnly;
}

using dotenv.net;

namespace ProgramInviterChanger;

internal sealed record AppSettings(
    Uri ApiBaseUrl,
    string ConnectionString,
    TimeSpan RequestDelay)
{
    public static AppSettings Load()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "ProgramInviterChanger", ".env"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"))
        };
        var envFile = candidates.FirstOrDefault(File.Exists);
        if (envFile is not null)
        {
            DotEnv.Load(new DotEnvOptions(
                envFilePaths: [envFile],
                ignoreExceptions: false,
                overwriteExistingVars: false));
        }

        var apiBaseUrl = Environment.GetEnvironmentVariable(
                "PROGRAM_INVITER_CHANGER_API_BASE_URL")
            ?? "http://localhost:5004";
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri))
            throw new InvalidOperationException("The contracts API base URL is invalid.");

        var connectionString = Environment.GetEnvironmentVariable(
            "PROGRAM_INVITER_CHANGER_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "PROGRAM_INVITER_CHANGER_CONNECTION_STRING is required.");
        }

        var delayText = Environment.GetEnvironmentVariable(
                "PROGRAM_INVITER_CHANGER_REQUEST_DELAY_MS")
            ?? "120";
        if (!int.TryParse(delayText, out var delayMs) || delayMs < 0)
            throw new InvalidOperationException("The contracts API request delay is invalid.");

        return new AppSettings(
            EnsureTrailingSlash(apiUri),
            connectionString,
            TimeSpan.FromMilliseconds(delayMs));
    }

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        var value = uri.ToString();
        return value.EndsWith("/", StringComparison.Ordinal)
            ? uri
            : new Uri(value + '/', UriKind.Absolute);
    }
}

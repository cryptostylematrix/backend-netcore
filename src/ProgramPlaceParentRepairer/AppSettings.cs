using dotenv.net;

namespace ProgramPlaceParentRepairer;

internal static class AppSettings
{
    public static string LoadConnectionString()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "ProgramPlaceParentRepairer", ".env"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"))
        };
        var file = candidates.FirstOrDefault(File.Exists);
        if (file is not null)
            DotEnv.Load(new DotEnvOptions(envFilePaths: [file], ignoreExceptions: false, overwriteExistingVars: false));
        var connectionString = Environment.GetEnvironmentVariable("PROGRAM_PLACE_PARENT_REPAIRER_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ParentRepairException("PROGRAM_PLACE_PARENT_REPAIRER_CONNECTION_STRING is required.");
        return connectionString;
    }
}

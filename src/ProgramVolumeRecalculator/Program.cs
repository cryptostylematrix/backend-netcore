using dotenv.net;
using ProgramVolumeRecalculator;

try
{
    LoadEnvironmentFile(args);
    var options = RecalculationOptions.Parse(args);
    using var cancellationSource = new CancellationTokenSource();

    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellationSource.Cancel();
    };

    var recalculator = new VolumeRecalculator(options.ConnectionString);
    await recalculator.RunAsync(options, cancellationSource.Token);
    return 0;
}
catch (OptionsException exception)
{
    (exception.ShowUsageOnly ? Console.Out : Console.Error).WriteLine(exception.Message);
    return exception.ShowUsageOnly ? 0 : 2;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Volume recalculation cancelled.");
    return 130;
}
catch (Exception exception)
{
    Console.Error.WriteLine("Volume recalculation failed: {0}", exception.Message);
    return 1;
}

static void LoadEnvironmentFile(string[] args)
{
    string? requestedEnvFile = null;
    for (var index = 0; index < args.Length; index++)
    {
        if (!args[index].Equals("--env-file", StringComparison.OrdinalIgnoreCase))
            continue;
        if (requestedEnvFile is not null)
            throw new InvalidOperationException("--env-file can be supplied only once.");
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            throw new InvalidOperationException("--env-file requires a path.");
        requestedEnvFile = args[index + 1];
    }

    var candidates = requestedEnvFile is not null
        ? [Path.GetFullPath(requestedEnvFile, Directory.GetCurrentDirectory())]
        : new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "ProgramVolumeRecalculator", ".env"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"))
        };
    var envFile = candidates.FirstOrDefault(File.Exists);
    if (envFile is null && requestedEnvFile is not null)
        throw new InvalidOperationException($"Environment file '{requestedEnvFile}' was not found.");
    if (envFile is null)
        return;

    DotEnv.Load(new DotEnvOptions(
        envFilePaths: [envFile],
        ignoreExceptions: false,
        overwriteExistingVars: requestedEnvFile is not null));
    Console.WriteLine("Loaded settings from {0}", envFile);
}

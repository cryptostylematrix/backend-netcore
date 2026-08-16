using ProgramMigrator;
using dotenv.net;

try
{
    const uint multiProgramId = 0x1ce8c484;
    const uint neoProgramId = 0x435acabf;

    LoadEnvironmentFile(args);
    var options = MigrationOptions.Parse(args);
    var progress = new ConsoleMigrationProgress();
    using var cancellationSource = new CancellationTokenSource();

    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellationSource.Cancel();
    };

    var legacyProgramType = options.ProgramId switch
    {
        multiProgramId => LegacyProgramType.Multi,
        neoProgramId => LegacyProgramType.Neo,
        _ => throw new InvalidOperationException(
            $"Program {options.ProgramId:X8} is not supported. Only Multi and Neo can be migrated.")
    };

    ProgramMigrationData migration;
    string? rootProfileAddr = null;
    string? rootInviteAddr = null;

    if (options.Scope == MigrationScope.Invite)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = EnsureTrailingSlash(options.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(60)
        };
        var contracts = new ContractApiClient(httpClient, options.RequestDelay);
        var loader = new InviteTreeLoader(
            contracts,
            options.ProgramId,
            options.MaxInvites,
            progress);

        Console.WriteLine("Loading program {0:X8} Invite structure...", options.ProgramId);
        var inviteImport = await loader.LoadAsync(
            options.RootProfileAddr,
            options.RootProfileLogin,
            cancellationSource.Token);
        rootProfileAddr = inviteImport.RootProfileAddr;
        rootInviteAddr = inviteImport.RootInviteAddr;
        migration = new ProgramMigrationData(inviteImport.Nodes, []);
    }
    else
    {
        Console.WriteLine("Loading legacy {0} structures 1+ and locks...", legacyProgramType);
        var legacyReader = new LegacyStructureReader(
            options.SourceConnectionString,
            progress);
        var legacyImport = await legacyReader.LoadAsync(
            legacyProgramType,
            options.SourceMarketingAddr,
            cancellationSource.Token);
        migration = new ProgramMigrationData(legacyImport.Places, legacyImport.Locks);
    }

    Console.WriteLine();
    Console.WriteLine("Migration data loaded successfully.");
    Console.WriteLine("Marketing:    {0}", options.MarketingAddr);
    Console.WriteLine("Program:      {0:X8} ({0})", options.ProgramId);
    Console.WriteLine("Scope:        {0}", options.Scope.ToString().ToLowerInvariant());
    if (options.Scope == MigrationScope.Invite)
    {
        Console.WriteLine("Root Profile: {0}", rootProfileAddr);
        Console.WriteLine("Root Invite:  {0}", rootInviteAddr);
    }
    Console.WriteLine("Places:       {0}", migration.Places.Count);
    Console.WriteLine("Locks:        {0}", migration.Locks.Count);
    Console.WriteLine("Structures:");
    foreach (var structure in migration.Places
                 .GroupBy(place => place.StructureNumber)
                 .OrderBy(group => group.Key))
    {
        var locksCount = migration.Locks.Count(positionLock =>
            positionLock.StructureNumber == structure.Key);
        Console.WriteLine(
            "  {0}: {1} places, {2} locks",
            structure.Key,
            structure.Count(),
            locksCount);
    }

    if (!options.ApplyChanges)
    {
        Console.WriteLine();
        Console.WriteLine("Dry run complete; no database changes were made.");
        Console.WriteLine("Run again with --apply to import this scope.");
        return 0;
    }

    Console.WriteLine();
    Console.WriteLine("Importing {0} scope into PostgreSQL...", options.Scope.ToString().ToLowerInvariant());
    var writer = new ProgramDataWriter(options.ConnectionString, progress);
    await writer.WriteAsync(
        options.MarketingAddr,
        migration,
        options.Scope,
        cancellationSource.Token);

    Console.WriteLine("Migration scope imported successfully.");
    return 0;
}
catch (OptionsException exception)
{
    if (exception.ShowUsageOnly)
        Console.WriteLine(exception.Message);
    else
        Console.Error.WriteLine(exception.Message);

    return exception.ShowUsageOnly ? 0 : 2;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Migration cancelled.");
    return 130;
}
catch (Exception exception)
{
    Console.Error.WriteLine("Migration failed: {0}", exception.Message);
    return 1;
}

static Uri EnsureTrailingSlash(Uri uri)
{
    var value = uri.ToString();
    return value.EndsWith("/", StringComparison.Ordinal)
        ? uri
        : new Uri(value + '/', UriKind.Absolute);
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
        Path.Combine(Directory.GetCurrentDirectory(), "src", "ProgramMigrator", ".env"),
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
    Console.WriteLine("Loaded migrator settings from {0}", envFile);
}

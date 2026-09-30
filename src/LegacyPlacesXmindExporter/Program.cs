using dotenv.net;
using LegacyPlacesXmindExporter;
using Npgsql;

if (args.Length == 1 && args[0] is "--help" or "-h")
{
    Console.WriteLine(ExportOptions.Usage);
    return 0;
}

try
{
    var options = ExportOptions.Parse(args);
    var output = options.Output;
    if (options.EnvFile is not null)
        DotEnv.Load(new DotEnvOptions(envFilePaths: [Path.GetFullPath(options.EnvFile)],
            ignoreExceptions: false, overwriteExistingVars: false));
    var connectionString = Environment.GetEnvironmentVariable("LEGACY_PLACES_CONNECTION_STRING");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new ArgumentException("Задайте LEGACY_PLACES_CONNECTION_STRING в окружении или --env-file.");
    try { _ = new NpgsqlConnectionStringBuilder(connectionString); }
    catch (ArgumentException) { throw new ArgumentException("Некорректный формат LEGACY_PLACES_CONNECTION_STRING."); }

    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
    Console.WriteLine("Чтение мест структуры {0}...", options.Structure);
    using var progress = new ExportProgress(Console.Out);
    var places = await PlaceReader.ReadAsync(connectionString, options.Structure, cancellation.Token, progress);
    var roots = PlaceHierarchy.Build(places, cancellation.Token, progress);
    var placeCount = places.Count;
    places.Clear(); // The tree owns the records now; release the redundant references.
    XmindWriter.Write(output, roots, cancellation.Token, progress, placeCount);
    Console.WriteLine("Готово: {0}. Мест: {1}, листов: {2}.", output, placeCount, roots.Count);
    return 0;
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine(exception.Message);
    return 2;
}
catch (InvalidDataException exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Экспорт отменён.");
    return 130;
}
catch (NpgsqlException exception)
{
    Console.Error.WriteLine(DatabaseError.Describe(exception));
    return 1;
}
catch (Exception exception)
{
    // Driver/IO exception messages may contain connection details or source data.
    Console.Error.WriteLine("Экспорт не выполнен ({0}). Проверьте подключение и доступность выходного каталога.", exception.GetType().Name);
    return 1;
}

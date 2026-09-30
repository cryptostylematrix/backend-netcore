using System.Globalization;

namespace LegacyPlacesXmindExporter;

public sealed record ExportOptions(short Structure, string Output, string? EnvFile)
{
    public const string Usage = """
        Экспорт public.places старой PostgreSQL-базы в XMind.
        Номер структуры и имя файла запрашиваются при запуске.
        --env-file <path>       Загрузить указанный .env файл.
        --help                 Показать справку.
        Подключение: переменная LEGACY_PLACES_CONNECTION_STRING.
        Существующие файлы не перезаписываются. База используется только для чтения.
        """;

    public static ExportOptions Parse(string[] args, TextReader? input = null, TextWriter? prompt = null)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            var key = args[i];
            if (key != "--env-file")
                throw new ArgumentException("Неизвестный параметр. Используйте --help.");
            if (++i == args.Length || string.IsNullOrWhiteSpace(args[i]) || args[i].StartsWith("--"))
                throw new ArgumentException($"Не задано значение {key}.");
            if (!values.TryAdd(key, args[i]))
                throw new ArgumentException($"Параметр {key} задан несколько раз.");
        }

        input ??= Console.In;
        prompt ??= Console.Out;
        short structure;
        while (true)
        {
            prompt.Write("Номер структуры: ");
            prompt.Flush();
            var value = input.ReadLine() ?? throw new ArgumentException("Не получен номер структуры: ввод завершён.");
            if (short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out structure))
                break;
            prompt.WriteLine("Введите целое число от -32768 до 32767.");
        }

        var defaultName = $"places-{structure}.xmind";
        while (true)
        {
            prompt.Write($"Имя выходного файла [{defaultName}]: ");
            prompt.Flush();
            var filename = input.ReadLine() ?? throw new ArgumentException("Не получено имя файла: ввод завершён.");
            try
            {
                var output = ValidateOutput(string.IsNullOrWhiteSpace(filename) ? defaultName : filename.Trim());
                return new ExportOptions(structure, output, values.GetValueOrDefault("--env-file"));
            }
            catch (ArgumentException exception)
            {
                prompt.WriteLine(exception.Message);
            }
        }
    }

    private static string ValidateOutput(string filename)
    {
        var output = Path.GetFullPath(filename);
        if (!string.Equals(Path.GetExtension(output), ".xmind", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Выходной файл должен иметь расширение .xmind.");
        if (File.Exists(output))
            throw new ArgumentException("Выходной файл уже существует. Укажите другое имя.");
        return output;
    }
}

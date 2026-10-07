using System.IO.Compression;
using System.Text.Json;
using LegacyPlacesXmindExporter;
using Xunit;

namespace LegacyPlacesXmindExporter.Tests;

public sealed class ExportTests
{
    private static Place Make(long id, long? parent = null, short pos = 0, short type = 0) =>
        new(id, parent, pos, type, "профиль-\"тест\"\\место", new DateTime(2020, 2, 3, 4, 5, 6).AddTicks(1234560));

    [Fact]
    public void ArchivePreservesHierarchyOrderTitlesAndSeparateRoots()
    {
        var roots = PlaceHierarchy.Build([Make(4, 1, 2), Make(2, 1, 0, 1), Make(3, 2), Make(1), Make(5)]);
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xmind");
        try
        {
            XmindWriter.Write(path, roots);
            using var archive = ZipFile.OpenRead(path);
            Assert.Equal(3, archive.Entries.Count);
            using var content = JsonDocument.Parse(archive.GetEntry("content.json")!.Open());
            var sheets = content.RootElement;
            Assert.Equal(2, sheets.GetArrayLength());
            Assert.Equal(Make(1).Login, sheets[0].GetProperty("title").GetString());
            Assert.Equal(Make(5).Login, sheets[1].GetProperty("title").GetString());
            var root = sheets[0].GetProperty("rootTopic");
            Assert.Equal("place-1", root.GetProperty("id").GetString());
            Assert.Equal("org.xmind.ui.org-chart.down", root.GetProperty("structureClass").GetString());
            var children = root.GetProperty("children").GetProperty("attached");
            Assert.Equal("place-2", children[0].GetProperty("id").GetString());
            Assert.Equal("place-4", children[1].GetProperty("id").GetString());
            Assert.Equal("[2]\n" + Make(2).Login + "\n03.02.20 07:05:06", children[0].GetProperty("title").GetString());
            Assert.EndsWith("\nclone", children[1].GetProperty("title").GetString());
            Assert.Equal("place-3", children[0].GetProperty("children").GetProperty("attached")[0].GetProperty("id").GetString());
            using var manifest = JsonDocument.Parse(archive.GetEntry("manifest.json")!.Open());
            foreach (var entry in manifest.RootElement.GetProperty("file-entries").EnumerateObject())
                Assert.NotNull(archive.GetEntry(entry.Name));
            using var metadata = JsonDocument.Parse(archive.GetEntry("metadata.json")!.Open());
            Assert.Equal("2", metadata.RootElement.GetProperty("dataStructureVersion").GetString());
        }
        finally { File.Delete(path); }
    }

    public static IEnumerable<object[]> InvalidTrees()
    {
        yield return [Array.Empty<Place>()];
        yield return [new[] { Make(1, 99) }];
        yield return [new[] { Make(1, 1) }];
        yield return [new[] { Make(1), Make(2, 3), Make(3, 2) }];
        yield return [new[] { Make(1), Make(2, 1), Make(3, 1) }];
        yield return [new[] { Make(1), Make(1) }];
        yield return [new[] { Make(1, type: 2) }];
        yield return [new[] { Make(1, pos: -1) }];
    }

    [Theory]
    [MemberData(nameof(InvalidTrees))]
    public void InvalidHierarchyFailsWithoutSilentlyDroppingPlaces(Place[] places) =>
        Assert.Throws<InvalidDataException>(() => PlaceHierarchy.Build(places));

    [Theory]
    [InlineData(1001)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    public void DeepTreeIsWrittenWithoutRecursiveTraversal(int depth)
    {
        var places = Enumerable.Range(1, depth)
            .Select(i => Make(i, i == 1 ? null : i - 1)).ToArray();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xmind");
        try
        {
            XmindWriter.Write(path, PlaceHierarchy.Build(places));
            using var zip = ZipFile.OpenRead(path);
            using var content = zip.GetEntry("content.json")!.Open();
            using var buffer = new MemoryStream();
            content.CopyTo(buffer);
            // Token validation avoids recursive traversal and deep-DOM construction costs.
            var reader = new Utf8JsonReader(buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length)),
                new JsonReaderOptions { MaxDepth = int.MaxValue });
            var topics = 0;
            var maximumJsonDepth = 0;
            while (reader.Read())
            {
                maximumJsonDepth = Math.Max(maximumJsonDepth, reader.CurrentDepth);
                if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("id"))
                {
                    Assert.True(reader.Read());
                    var id = reader.GetString()!;
                    if (id.StartsWith("place-", StringComparison.Ordinal))
                        Assert.Equal($"place-{++topics}", id);
                }
            }
            Assert.Equal(depth, topics);
            Assert.True(maximumJsonDepth >= 3 * depth - 1);
            Assert.Equal(JsonTokenType.EndArray, reader.TokenType);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ExistingFileAndCancellationLeaveNoPartialOutput()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "output.xmind");
        try
        {
            var roots = PlaceHierarchy.Build([Make(1)]);
            File.WriteAllText(path, "original");
            Assert.Throws<IOException>(() => XmindWriter.Write(path, roots));
            Assert.Equal("original", File.ReadAllText(path));
            File.Delete(path);
            Assert.Throws<OperationCanceledException>(() => XmindWriter.Write(path, roots, new CancellationToken(true)));
            Assert.Empty(Directory.GetFiles(directory));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData("--structure", "5")]
    [InlineData("--output", "places.xmind")]
    [InlineData("--env-file")]
    [InlineData("--env-file", "a", "--env-file", "b")]
    [InlineData("--unknown", "x")]
    public void InvalidOptionsAreRejected(params string[] args) =>
        Assert.Throws<ArgumentException>(() => ExportOptions.Parse(args, new StringReader(""), TextWriter.Null));

    [Theory]
    [InlineData("0", (short)0)]
    [InlineData("5", (short)5)]
    [InlineData("32767", short.MaxValue)]
    [InlineData("-32768", short.MinValue)]
    public void StartupPromptsForStructureAndFilename(string value, short expected)
    {
        using var prompt = new StringWriter();
        var filename = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xmind");
        var options = ExportOptions.Parse(["--env-file", "settings.env"], new StringReader(value + "\n\n" + filename + "\n"), prompt);
        Assert.Equal(expected, options.Structure);
        Assert.Equal(filename, options.Output);
        Assert.Equal("settings.env", options.EnvFile);
        Assert.Contains("Номер структуры:", prompt.ToString());
        Assert.Contains($"places-{expected}.xmind", prompt.ToString());
    }

    [Fact]
    public void InvalidAnswersAreRetried()
    {
        var filename = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xmind");
        using var prompt = new StringWriter();
        var options = ExportOptions.Parse([], new StringReader("abc\n32768\n5\n\nbad.zip\n" + filename + "\n"), prompt);
        Assert.Equal((short)5, options.Structure);
        Assert.Equal(filename, options.Output);
        Assert.Contains("Введите целое число", prompt.ToString());
        Assert.Contains("расширение .xmind", prompt.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("5\n")]
    public void EndOfInputIsRejected(string input) =>
        Assert.Throws<ArgumentException>(() => ExportOptions.Parse([], new StringReader(input), TextWriter.Null));

    [Fact]
    public void MillionPlaceExportReportsCompletedStagesAndCreatesReadableArchive()
    {
        const int count = 1_000_001;
        var places = Enumerable.Range(1, count)
            .Select(i => Make(i, i == 1 ? null : i / 2, (short)(i % 2))).ToArray();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xmind");
        using var log = new StringWriter();
        try
        {
            using (var progress = new ExportProgress(log))
            {
                var roots = PlaceHierarchy.Build(places, progress: progress);
                XmindWriter.Write(path, roots, progress: progress, totalPlaces: count);
            }
            var output = log.ToString();
            foreach (var stage in new[] { "Индексация мест", "Построение связей", "Сортировка позиций", "Проверка дерева", "Запись XMind" })
                Assert.Contains($"{stage}: 1,000,001 / 1,000,001", output);
            Assert.Contains("мест/с; завершено", output);
            using var archive = ZipFile.OpenRead(path);
            // Stream decompression without loading a million-topic JSON DOM into memory.
            using var content = archive.GetEntry("content.json")!.Open();
            content.CopyTo(Stream.Null);
            Assert.True(archive.GetEntry("content.json")!.Length > count * 100L);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(System.Net.Sockets.SocketError.HostNotFound, "hostname could not be resolved")]
    [InlineData(System.Net.Sockets.SocketError.ConnectionRefused, "connection was refused")]
    [InlineData(System.Net.Sockets.SocketError.TimedOut, "timed out")]
    [InlineData(System.Net.Sockets.SocketError.HostUnreachable, "host is unreachable")]
    public void ConnectionDiagnosticsIdentifySocketFailures(System.Net.Sockets.SocketError code, string expected)
    {
        var error = new Npgsql.NpgsqlException("Password=secret", new IOException("secret",
            new System.Net.Sockets.SocketException((int)code)));
        var message = DatabaseError.Describe(error);
        Assert.Contains(expected, message);
        Assert.DoesNotContain("secret", message);
    }

    [Fact]
    public void ConnectionDiagnosticsDistinguishTlsAndTimeoutWithoutLeakingMessages()
    {
        Assert.Contains("TLS", DatabaseError.Describe(new Npgsql.NpgsqlException("secret",
            new System.Security.Authentication.AuthenticationException("secret"))));
        Assert.Contains("timed out", DatabaseError.Describe(new Npgsql.NpgsqlException("secret", new TimeoutException("secret"))));
        var fallback = DatabaseError.Describe(new Npgsql.NpgsqlException("secret", new IOException("secret")));
        Assert.Contains("IOException", fallback);
        Assert.DoesNotContain("secret", fallback);
        var serverError = DatabaseError.Describe(new Npgsql.PostgresException("secret", "FATAL", "FATAL", "28P01"));
        Assert.Contains("28P01", serverError);
        Assert.DoesNotContain("secret", serverError);
    }

    [Theory]
    [InlineData(2020, 2, 3, 4, 5, 6, "03.02.20 07:05:06")]
    [InlineData(2025, 12, 31, 22, 15, 0, "01.01.26 01:15:00")]
    [InlineData(2012, 1, 1, 0, 0, 0, "01.01.12 04:00:00")]
    public void ExportConvertsStoredUtcToMoscow(int year, int month, int day, int hour, int minute, int second, string expected)
    {
        // PostgreSQL timestamp without time zone is read as DateTimeKind.Unspecified.
        var place = Make(1) with { CreatedAt = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified) };
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xmind");
        try
        {
            XmindWriter.Write(path, PlaceHierarchy.Build([place]));
            using var zip = ZipFile.OpenRead(path);
            using var stream = zip.GetEntry("content.json")!.Open();
            using var json = JsonDocument.Parse(stream);
            Assert.Equal($"[{place.Id}]\n{place.Login}\n{expected}\nclone",
                json.RootElement[0].GetProperty("rootTopic").GetProperty("title").GetString());
            Assert.Equal(DateTimeKind.Unspecified, place.CreatedAt.Kind);
            Assert.Equal(hour, place.CreatedAt.Hour);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void SubtreePromptRetriesInvalidIdAndUsesRootInDefaultFilename()
    {
        using var prompt = new StringWriter();
        var options = ExportOptions.Parse([], new StringReader("5\ninvalid\n9223372036854775808\n12345\n\n"), prompt);
        Assert.Equal(12345L, options.RootPlaceId);
        Assert.EndsWith("places-5-from-12345.xmind", options.Output);
    }

    [Fact]
    public void SubtreeRootMayHaveParentOutsideExportButCyclesAreRejected()
    {
        var roots = PlaceHierarchy.Build([Make(20, 10), Make(22, 20, 1), Make(21, 20, 0)], rootPlaceId: 20);
        Assert.Equal(20, Assert.Single(roots).Place.Id);
        Assert.Equal(new long[] { 21, 22 }, roots[0].Children.Select(n => n.Place.Id));
        Assert.Equal(20, Assert.Single(PlaceHierarchy.Build([Make(20, 10)], rootPlaceId: 20)).Place.Id);
        Assert.Throws<InvalidDataException>(() => PlaceHierarchy.Build([Make(20, 21), Make(21, 20)], rootPlaceId: 20));
        Assert.Throws<InvalidDataException>(() => PlaceHierarchy.Build([Make(20, 20)], rootPlaceId: 20));
        Assert.Throws<InvalidDataException>(() => PlaceHierarchy.Build([Make(20)], rootPlaceId: 99));
    }

}

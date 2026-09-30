using System.Globalization;
using System.IO.Compression;
using System.Text.Json;

namespace LegacyPlacesXmindExporter;

public static class XmindWriter
{
    private static readonly TimeZoneInfo MoscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");

    public static void Write(string output, IReadOnlyList<PlaceNode> roots, CancellationToken cancellationToken = default, ExportProgress? progress = null, long? totalPlaces = null)
    {
        progress?.Start("Запись XMind", totalPlaces);
        var fullPath = Path.GetFullPath(output);
        var temporaryPath = fullPath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            using (var file = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
            {
                WriteJson(archive, "metadata.json", writer =>
                {
                    writer.WriteStartObject();
                    writer.WriteString("dataStructureVersion", "2");
                    writer.WriteStartObject("creator");
                    writer.WriteString("name", "LegacyPlacesXmindExporter");
                    writer.WriteString("version", "1.0.0");
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                });
                WriteJson(archive, "manifest.json", writer =>
                {
                    writer.WriteStartObject();
                    writer.WriteStartObject("file-entries");
                    foreach (var name in new[] { "content.json", "metadata.json" })
                    {
                        writer.WriteStartObject(name);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                });
                WriteJson(archive, "content.json", writer =>
                {
                    writer.WriteStartArray();
                    foreach (var root in roots)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("id", $"sheet-{root.Place.Id}");
                        writer.WriteString("class", "sheet");
                        writer.WriteString("title", root.Place.Login);
                        XmindTheme.Write(writer, $"sheet-{root.Place.Id}");
                        writer.WritePropertyName("rootTopic");
                        WriteTree(writer, root, cancellationToken, progress);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                });
            }
            progress?.Complete();
            progress?.Start("Сохранение архива");
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, overwrite: false);
            progress?.Complete();
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static void WriteJson(ZipArchive archive, string name, Action<Utf8JsonWriter> write)
    {
        using var stream = archive.CreateEntry(name, CompressionLevel.Optimal).Open();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { MaxDepth = int.MaxValue });
        write(writer);
    }

    private static void WriteTree(Utf8JsonWriter writer, PlaceNode root, CancellationToken cancellationToken, ExportProgress? progress)
    {
        var pending = new Stack<(PlaceNode Node, bool Close)>();
        pending.Push((root, false));
        while (pending.TryPop(out var item))
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Utf8JsonWriter otherwise retains the entire JSON document until disposal.
            if (writer.BytesPending >= 64 * 1024)
                writer.Flush();
            var node = item.Node;
            if (item.Close)
            {
                if (node.Children.Count > 0)
                {
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
                progress?.Advance();
                continue;
            }
            var place = node.Place;
            // The legacy timestamp has no zone metadata, but its stored values are UTC.
            var utcCreatedAt = DateTime.SpecifyKind(place.CreatedAt, DateTimeKind.Utc);
            var date = TimeZoneInfo.ConvertTimeFromUtc(utcCreatedAt, MoscowTimeZone)
                .ToString("dd.MM.yy HH:mm:ss", CultureInfo.InvariantCulture);
            writer.WriteStartObject();
            writer.WriteString("id", $"place-{place.Id}");
            writer.WriteString("class", "topic");
            writer.WriteString("title", $"[{place.Id.ToString(CultureInfo.InvariantCulture)}]\n{place.Login}\n{date}{(place.Type == 0 ? "\nclone" : string.Empty)}");
            writer.WriteString("structureClass", XmindTheme.StructureClass);
            pending.Push((node, true));
            if (node.Children.Count > 0)
            {
                writer.WriteStartObject("children");
                writer.WriteStartArray("attached");
                for (var i = node.Children.Count - 1; i >= 0; i--)
                    pending.Push((node.Children[i], false));
            }
        }
    }
}

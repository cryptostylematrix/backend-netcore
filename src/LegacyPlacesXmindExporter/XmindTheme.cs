using System.Text.Json;

namespace LegacyPlacesXmindExporter;

internal static class XmindTheme
{
    public const string StructureClass = "org.xmind.ui.org-chart.down";

    // One shared theme per sheet keeps per-topic overhead small for large exports.
    public static void Write(Utf8JsonWriter writer, string sheetId)
    {
        writer.WriteStartObject("theme");
        writer.WriteString("id", $"{sheetId}-theme");
        writer.WriteString("title", "Teal hierarchy");
        writer.WriteStartObject("map");
        writer.WriteString("type", "map");
        writer.WriteString("styleId", $"{sheetId}-map");
        writer.WriteStartObject("properties");
        writer.WriteString("svg:fill", "#4C9C9E");
        writer.WriteEndObject();
        writer.WriteEndObject();
        WriteTopicStyle(writer, sheetId, "centralTopic", "#16465D", "#FFFFFF", 20, "600", "roundedRect");
        WriteTopicStyle(writer, sheetId, "mainTopic", "#EEEBD7", "#243D43", 14, "600", "roundedRect");
        WriteTopicStyle(writer, sheetId, "subTopic", "none", "#FFFFFF", 11, "400", "underline");
        writer.WriteEndObject();
    }

    private static void WriteTopicStyle(Utf8JsonWriter writer, string sheetId, string role,
        string fill, string color, int fontSize, string fontWeight, string shape)
    {
        writer.WriteStartObject(role);
        writer.WriteString("type", "topic");
        writer.WriteString("styleId", $"{sheetId}-{role}");
        writer.WriteStartObject("properties");
        writer.WriteString("fo:font-family", "Arial");
        writer.WriteNumber("fo:font-size", fontSize);
        writer.WriteString("fo:font-weight", fontWeight);
        writer.WriteString("fo:font-style", "normal");
        writer.WriteString("fo:text-align", "center");
        writer.WriteString("fo:color", color);
        writer.WriteString("svg:fill", fill);
        writer.WriteString("shape-class", $"org.xmind.topicShape.{shape}");
        writer.WriteString("border-line-width", "0");
        writer.WriteString("line-class", "org.xmind.branchConnection.roundedElbow");
        writer.WriteString("line-color", "#E8EBD7");
        writer.WriteString("line-width", "1");
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}

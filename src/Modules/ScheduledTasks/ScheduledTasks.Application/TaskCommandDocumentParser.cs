using System.Text.Json;

namespace ScheduledTasks.Application;

public sealed class TaskCommandDocumentParser
{
    public IReadOnlyCollection<string> GetProgramMarketingAddresses(string commands)
    {
        using var document = JsonDocument.Parse(commands);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            return [];

        return document.RootElement
            .EnumerateArray()
            .Where(command => command.ValueKind == JsonValueKind.Object)
            .Where(command => command.TryGetProperty("module", out var module)
                && module.ValueKind == JsonValueKind.String
                && string.Equals(module.GetString(), "program", StringComparison.Ordinal))
            .Select(command => command.TryGetProperty("target", out var target)
                && target.ValueKind == JsonValueKind.Object
                && target.TryGetProperty("marketingAddress", out var address)
                && address.ValueKind == JsonValueKind.String
                    ? address.GetString()
                    : null)
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Select(address => address!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<TaskCommandEnvelope> Parse(string commands)
    {
        using var document = JsonDocument.Parse(commands);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new FormatException("Task commands must be a JSON array.");

        var result = new List<TaskCommandEnvelope>();
        var sequence = 0;
        foreach (var command in document.RootElement.EnumerateArray())
        {
            if (command.ValueKind != JsonValueKind.Object)
                throw new FormatException($"Command {sequence} must be a JSON object.");

            var module = GetRequiredString(command, "module", sequence);
            var type = GetRequiredString(command, "type", sequence);
            var version = command.TryGetProperty("version", out var versionElement)
                && versionElement.TryGetInt32(out var parsedVersion)
                ? parsedVersion
                : 1;
            if (version <= 0)
                throw new FormatException($"Command {sequence} has an invalid version.");

            JsonElement targetDocument;
            if (command.TryGetProperty("target", out var target))
            {
                if (target.ValueKind != JsonValueKind.Object)
                    throw new FormatException($"Command {sequence} target must be an object.");

                targetDocument = target.Clone();
            }
            else
                targetDocument = JsonDocument.Parse("{}").RootElement.Clone();

            var arguments = command.TryGetProperty("arguments", out var argumentsElement)
                ? argumentsElement.Clone()
                : JsonDocument.Parse("{}").RootElement.Clone();

            result.Add(new TaskCommandEnvelope(
                sequence++,
                module,
                type,
                version,
                targetDocument,
                arguments));
        }

        if (result.Count == 0)
            throw new FormatException("A task must contain at least one command.");

        return result;
    }

    private static string GetRequiredString(
        JsonElement element,
        string propertyName,
        int sequence)
    {
        if (!element.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new FormatException(
                $"Command {sequence} must contain a non-empty '{propertyName}'.");
        }

        return value.GetString()!;
    }
}

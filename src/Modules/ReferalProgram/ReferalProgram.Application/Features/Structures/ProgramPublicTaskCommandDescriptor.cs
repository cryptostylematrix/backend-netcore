using System.Globalization;
using System.Text.Json;
using IntegrationRequests.Scheduling;
using IntegrationRequests;

namespace ReferalProgram.Application.Features.Structures;

public sealed class ProgramPublicTaskCommandDescriptor : IPublicTaskCommandDescriptor
{
    public string Module => "program";

    public string BuildCommandFilter(string scope) => JsonSerializer.Serialize(
        new[] { new { module = Module, target = new { marketingAddress = scope } } });

    public PublicScheduledAction? Describe(JsonElement command)
    {
        if (Text(command, "module") != Module
            || Text(command, "type") is not { } type
            || type is not ("program.structure.update-activity" or "program.structure.compress"
                or "program.structure.calculate-referral-volume" or "program.structure.reset-referral-volume"
                or DeactivateExpiredFirstPlacesRequest.CommandType)
            || (command.TryGetProperty("version", out var version)
                && (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var v) || v != 1))
            || !command.TryGetProperty("target", out var target)
            || Text(target, "marketingAddress") is not { Length: > 0 } scope
            || !command.TryGetProperty("arguments", out var arguments)
            || arguments.ValueKind != JsonValueKind.Object
            || !arguments.TryGetProperty("structureNumber", out var number)
            || number.ValueKind != JsonValueKind.Number
            || !number.TryGetInt32(out var structure) || structure is < 0 or > 255)
            return null;
        return new(type, new(Module, scope, "structure", structure.ToString(CultureInfo.InvariantCulture)));
    }

    private static string? Text(JsonElement value, string key) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(key, out var item)
            && item.ValueKind == JsonValueKind.String ? item.GetString() : null;
}

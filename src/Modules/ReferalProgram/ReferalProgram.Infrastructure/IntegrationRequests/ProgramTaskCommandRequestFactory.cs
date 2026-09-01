using IntegrationRequests;
using MessageBroker.Abstractions;
using ScheduledTasks.Application;
using System.Text.Json;

namespace ReferalProgram.Infrastructure.IntegrationRequests;

internal sealed class ProgramTaskCommandRequestFactory : ITaskCommandRequestFactory
{
    private static readonly HashSet<string> SupportedTypes =
    [
        "program.task-processing.disable",
        "program.task-processing.enable",
        "program.structure.update-activity",
        "program.structure.compress",
        "program.structure.calculate-personal-volume",
        "program.structure.reset-personal-volume"
    ];

    public bool CanCreate(TaskCommandEnvelope command) =>
        command.Version == 1
        && string.Equals(command.Module, "program", StringComparison.Ordinal)
        && SupportedTypes.Contains(command.Type);

    public IIntegrationRequest Create(
        TaskCommandEnvelope command,
        Guid correlationId,
        DateTime occurredOnUtc)
    {
        if (!command.Target.TryGetProperty("marketingAddress", out var marketingElement)
            || marketingElement.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(marketingElement.GetString()))
        {
            throw new FormatException(
                $"Program command {command.Sequence} requires target.marketingAddress.");
        }
        var marketingAddress = marketingElement.GetString()!;

        if (command.Type == "program.task-processing.disable")
        {
            return new DisableProgramTaskProcessingRequest(
                marketingAddress,
                correlationId,
                occurredOnUtc);
        }

        if (command.Type == "program.task-processing.enable")
        {
            return new EnableProgramTaskProcessingRequest(
                marketingAddress,
                correlationId,
                occurredOnUtc);
        }

        if (!command.Arguments.TryGetProperty("structureNumber", out var structureElement)
            || !structureElement.TryGetInt32(out var structureNumber)
            || structureNumber < 0)
        {
            throw new FormatException(
                $"Program command {command.Sequence} requires a non-negative arguments.structureNumber.");
        }

        return command.Type switch
        {
            "program.structure.update-activity" => new ResetStructureActivaityRequest(
                marketingAddress,
                structureNumber,
                correlationId,
                occurredOnUtc),
            "program.structure.compress" => new CompressStructureRequest(
                marketingAddress,
                structureNumber,
                correlationId,
                occurredOnUtc),
            "program.structure.calculate-personal-volume" =>
                new CalculateStructurePersonalVolumeRequest(
                    marketingAddress,
                    structureNumber,
                    correlationId,
                    occurredOnUtc),
            "program.structure.reset-personal-volume" =>
                new ResetStructurePersonalVolumeRequest(
                    marketingAddress,
                    structureNumber,
                    correlationId,
                    occurredOnUtc),
            _ => throw new InvalidOperationException(
                $"Unsupported Program task command '{command.Type}'.")
        };
    }
}

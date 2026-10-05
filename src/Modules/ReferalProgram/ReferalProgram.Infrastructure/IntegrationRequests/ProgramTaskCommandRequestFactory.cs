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
        DeactivateExpiredFirstPlacesRequest.CommandType,
        "program.structure.compress",
        "program.structure.calculate-referral-volume",
        "program.structure.reset-referral-volume"
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
            || structureElement.ValueKind != JsonValueKind.Number
            || !structureElement.TryGetInt32(out var structureNumber)
            || structureNumber < 0)
        {
            throw new FormatException(
                $"Program command {command.Sequence} requires a non-negative arguments.structureNumber.");
        }

        if (command.Type == DeactivateExpiredFirstPlacesRequest.CommandType)
        {
            if (structureNumber > byte.MaxValue)
                throw new FormatException("arguments.structureNumber must be between 0 and 255.");
            if (!command.Arguments.TryGetProperty("period", out var periodElement)
                || periodElement.ValueKind != JsonValueKind.Object
                || !periodElement.TryGetProperty("unit", out var unitElement)
                || unitElement.ValueKind != JsonValueKind.String
                || !periodElement.TryGetProperty("value", out var valueElement)
                || valueElement.ValueKind != JsonValueKind.Number
                || !valueElement.TryGetInt32(out var value))
                throw new FormatException("arguments.period requires a string unit and integer value.");

            var period = new ActivityExpirationPeriod(unitElement.GetString()!, value);
            period.GetCutoffUtc(occurredOnUtc);
            return new DeactivateExpiredFirstPlacesRequest(
                marketingAddress, structureNumber, period, correlationId, occurredOnUtc);
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
            "program.structure.calculate-referral-volume" =>
                new CalculateStructureReferralVolumeRequest(
                    marketingAddress,
                    structureNumber,
                    correlationId,
                    occurredOnUtc),
            "program.structure.reset-referral-volume" =>
                new ResetStructureReferralVolumeRequest(
                    marketingAddress,
                    structureNumber,
                    correlationId,
                    occurredOnUtc),
            _ => throw new InvalidOperationException(
                $"Unsupported Program task command '{command.Type}'.")
        };
    }
}

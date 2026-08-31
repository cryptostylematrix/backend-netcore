using System.Text.Json.Serialization;

namespace ReferalProgram.Application.Abstractions;

public sealed class ActivityConfiguration
{
    [JsonPropertyName("set_active_on_activation")]
    public bool SetActiveOnActivation { get; init; } = true;
}

public sealed record ActivatePlaceDecision(
    bool CanActivate,
    uint? CommandTag,
    bool SetActiveOnActivation,
    string? Reason);

public interface IActivatePlacePolicy
{
    ActivatePlaceDecision Evaluate(
        StructureResponse structure,
        IReadOnlySet<uint> availableCommandTags,
        PlaceResponse? place);

    Task<ActivatePlaceDecision> EvaluateAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        uint placeNumber,
        CancellationToken cancellationToken);
}

using System.Text.Json.Serialization;

namespace ReferalProgram.Application.Abstractions;

public sealed class ActivityConfiguration
{
    [JsonPropertyName("activation_sync")]
    public string? ActivationSync { get; init; }

    [JsonIgnore]
    public bool HasValidActivationSync =>
        ActivationSync is null or "structure" or "group" or "program";

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

using System.Text.Json;

namespace ReferalProgram.Application.Policies;

public sealed class ActivatePlacePolicy(
    IStructureQueries structureQueries,
    IPlaceQueries placeQueries,
    IProgramCommandQueries commandQueries) : IActivatePlacePolicy
{
    public ActivatePlaceDecision Evaluate(
        StructureResponse structure,
        IReadOnlySet<uint> availableCommandTags,
        PlaceResponse? place)
    {
        if (!availableCommandTags.Contains(ProgramCommandTags.ActivatePlace))
            return Denied("activation_command_not_configured");

        if (structure.Activity is null)
            return Denied("activity_configuration_missing");

        ActivitySettings configuration;
        try
        {
            configuration = ActivitySettings.Parse(structure.Activity.Value, structure.StructureNumber);
            if (configuration.ActivitySource == "group_root" && string.IsNullOrWhiteSpace(structure.Group))
                throw new JsonException("group_root activity requires a structure group.");
        }
        catch (JsonException)
        {
            return Denied("activity_configuration_invalid");
        }

        if (place is null)
            return Denied("place_not_found");

        if (string.IsNullOrWhiteSpace(place.ProfileAddr))
            return Denied("system_place_cannot_be_activated");

        if (place.ActivatedAt is not null)
            return Denied("place_already_activated");

        return new ActivatePlaceDecision(
            true,
            ProgramCommandTags.ActivatePlace,
            !configuration.PreserveStatusOnActivation,
            null);
    }

    public async Task<ActivatePlaceDecision> EvaluateAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        uint placeNumber,
        CancellationToken cancellationToken)
    {
        var structure = await structureQueries.GetStructureAsync(
            marketingAddr,
            structureNumber,
            cancellationToken);
        if (structure is null)
            return Denied("structure_not_found");

        var commands = await commandQueries.GetConfigurationAsync(
            marketingAddr,
            cancellationToken);
        var place = await placeQueries.GetPlaceAsync(
            marketingAddr,
            structureNumber,
            profileAddr,
            placeNumber,
            cancellationToken);

        return Evaluate(
            structure,
            commands.GetAvailableCommandTags(structureNumber),
            place);
    }

    private static ActivatePlaceDecision Denied(string reason) =>
        new(false, null, false, reason);
}

using System.Text.Json;
using Common.Domain;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Features.Places;

internal sealed class PlaceActivatedDomainEventHandler(
    IPlaceRepository placeRepository,
    IStructureQueries structureQueries,
    IProgramStructureListQueries structureListQueries)
    : IDomainEventHandler<PlaceActivatedDomainEvent>
{
    public async Task Handle(
        PlaceActivatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var source = await structureQueries.GetStructureAsync(
            notification.MarketingAddr, notification.StructureNumber, cancellationToken)
            ?? throw new InvalidOperationException("Activation structure was not found.");
        var configuration = ReadActivity(source);
        if (configuration?.ActivationSync is null)
            return;

        IReadOnlyList<StructureResponse> structures;
        if (configuration.ActivationSync == "structure"
            || (configuration.ActivationSync == "group" && source.Group is null))
        {
            structures = [source];
        }
        else
        {
            var programStructures = await structureListQueries.GetAsync(
                notification.MarketingAddr, cancellationToken);
            structures = programStructures.Where(structure =>
                structure.MarketingAddr == notification.MarketingAddr
                && (configuration.ActivationSync == "program"
                    || string.Equals(structure.Group, source.Group, StringComparison.Ordinal)))
                .ToArray();
        }

        var settings = structures.ToDictionary(
            structure => structure.StructureNumber,
            structure => ReadActivity(structure)?.SetActiveOnActivation ?? false);
        var places = await placeRepository.GetProfilePlacesAsync(
            notification.MarketingAddr, notification.ProfileAddr,
            settings.Keys.ToArray(), cancellationToken);
        foreach (var place in places)
        {
            if (place.StructureNumber == notification.StructureNumber
                && place.PlaceNumber == notification.PlaceNumber)
                continue;

            place.SynchronizeActivation(
                notification.ActivatedAt, settings[place.StructureNumber]);
        }
    }

    private static ActivityConfiguration? ReadActivity(StructureResponse structure)
    {
        var configuration = structure.Activity?.Deserialize<ActivityConfiguration>();
        if (configuration is not null && !configuration.HasValidActivationSync)
            throw new InvalidOperationException("Invalid activation_sync setting.");
        return configuration;
    }
}

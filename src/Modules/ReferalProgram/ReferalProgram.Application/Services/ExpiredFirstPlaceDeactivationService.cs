using IntegrationRequests;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Services;

public sealed class ExpiredFirstPlaceDeactivationService(
    IPlaceRepository placeRepository,
    IStructureQueries structureQueries)
{
    public async Task ExecuteAsync(
        DeactivateExpiredFirstPlacesRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.MarketingAddress))
            throw new FormatException("Marketing address is required.");
        if (request.StructureNumber is < byte.MinValue or > byte.MaxValue)
            throw new FormatException("Structure number is outside the byte range.");
        if (request.Period is null)
            throw new FormatException("Activity expiration period is required.");
        var cutoff = new DateTimeOffset(request.Period.GetCutoffUtc(request.OccurredOnUtc)).ToUnixTimeSeconds();
        var number = checked((byte)request.StructureNumber);
        if (await structureQueries.GetStructureAsync(request.MarketingAddress, number, cancellationToken) is null)
            throw new FormatException("Structure was not found.");

        // The repository applies the timestamp predicate in the UPDATE itself.
        // No activation/volume events or separate read-and-save window.
        await placeRepository.ExpireFirstPlacesAsync(request.MarketingAddress, number, cutoff, cancellationToken);
    }
}

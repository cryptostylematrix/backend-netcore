using Microsoft.EntityFrameworkCore;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Infrastructure.Persistence;

namespace ReferalProgram.Infrastructure.Repositories;

internal sealed class PlaceRepository(DataContext dataContext) : IPlaceRepository
{
    public Task<Place?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dataContext.Places.SingleOrDefaultAsync(place => place.Id == id, cancellationToken);

    public Task<Place?> GetAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        uint placeNumber,
        CancellationToken cancellationToken) =>
        dataContext.Places.SingleOrDefaultAsync(
            place => place.MarketingAddr == marketingAddr
                && place.StructureNumber == structureNumber
                && place.ProfileAddr == profileAddr
                && place.PlaceNumber == placeNumber,
            cancellationToken);

    public Task<Place?> GetByTaskKeyAsync(
        string marketingAddr,
        int taskKey,
        CancellationToken cancellationToken) =>
        dataContext.Places.FirstOrDefaultAsync(
            place => place.MarketingAddr == marketingAddr
                && place.TaskKey == taskKey,
            cancellationToken);

    public async Task<uint> GetNextPlaceNumberAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken)
    {
        var lastPlaceNumber = await dataContext.Places
            .Where(place => place.MarketingAddr == marketingAddr
                && place.StructureNumber == structureNumber
                && place.ProfileAddr == profileAddr)
            .OrderByDescending(place => place.PlaceNumber)
            .Select(place => (uint?)place.PlaceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return checked((lastPlaceNumber ?? 0) + 1);
    }

    public async Task<Place?> GetAncestorAsync(
        Place place,
        byte levels,
        CancellationToken cancellationToken)
    {
        var current = place;

        for (var level = 0; level < levels; level++)
        {
            if (current.ParentId is null)
                return null;

            var parent = await GetByIdAsync(current.ParentId.Value, cancellationToken);
            if (parent is null)
                return null;

            current = parent;
        }

        return current;
    }

    public Task<long> CountAtDepthAsync(
        string marketingAddr,
        byte structureNumber,
        string mpPrefix,
        uint depth,
        CancellationToken cancellationToken) =>
        dataContext.Places.LongCountAsync(
            place => place.MarketingAddr == marketingAddr
                && place.StructureNumber == structureNumber
                && place.Mp.StartsWith(mpPrefix)
                && place.Deep == depth,
            cancellationToken);

    public void Add(Place place) => dataContext.Places.Add(place);
}

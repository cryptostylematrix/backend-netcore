using Microsoft.EntityFrameworkCore;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Infrastructure.Persistence;

namespace ReferalProgram.Infrastructure.Repositories;

internal sealed class PlaceRepository(DataContext dataContext) : IPlaceRepository
{
    public async Task<IReadOnlyList<Place>> GetStructurePlacesAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken) =>
        await dataContext.Places
            .Where(place => place.MarketingAddr == marketingAddr
                && place.StructureNumber == structureNumber)
            .OrderBy(place => place.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<string, string?>> GetInvitersAsync(
        string marketingAddr,
        CancellationToken cancellationToken) =>
        await dataContext.Places
            .AsNoTracking()
            .Where(place => place.MarketingAddr == marketingAddr
                && place.StructureNumber == 0
                && place.PlaceNumber == 1
                && place.ProfileAddr != null)
            .ToDictionaryAsync(
                place => place.ProfileAddr!,
                place => place.ParentProfileAddr,
                StringComparer.Ordinal,
                cancellationToken);

    public Task<Place?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dataContext.Places.SingleOrDefaultAsync(place => place.Id == id, cancellationToken);

    public async Task<Place?> GetAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        uint placeNumber,
        CancellationToken cancellationToken)
    {
        var tracked = dataContext.Places.Local.SingleOrDefault(
            place => place.MarketingAddr == marketingAddr
                && place.StructureNumber == structureNumber
                && place.ProfileAddr == profileAddr
                && place.PlaceNumber == placeNumber);

        return tracked ?? await dataContext.Places.SingleOrDefaultAsync(
            place => place.MarketingAddr == marketingAddr
                && place.StructureNumber == structureNumber
                && place.ProfileAddr == profileAddr
                && place.PlaceNumber == placeNumber,
            cancellationToken);
    }

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

    public Task<long> CountCloneChildrenAsync(
        int parentId,
        CancellationToken cancellationToken) =>
        dataContext.Places.LongCountAsync(
            place => place.ParentId == parentId
                && (place.Kind == PlaceKinds.Clone
                    || place.Kind == PlaceKinds.TerminalClone),
            cancellationToken);

    public Task IncrementMatrixFillingForAncestorsAsync(
        int parentId,
        CancellationToken cancellationToken)
    {
        return dataContext.Database.ExecuteSqlInterpolatedAsync($$"""
            WITH structure_config AS MATERIALIZED
            (
                SELECT place.marketing_addr,
                       place.structure_number,
                       structure.width,
                       structure.height,
                       place.mp,
                       place.deep
                FROM public.places place
                JOIN public.structures structure
                  ON structure.marketing_addr = place.marketing_addr
                 AND structure.structure_number = place.structure_number
                WHERE place.id = {{parentId}}
                FOR SHARE OF structure
            )
            UPDATE public.places ancestor
            SET matrix_filling = ancestor.matrix_filling + 1
            FROM structure_config
            CROSS JOIN LATERAL generate_series(
                GREATEST(
                    structure_config.deep - structure_config.height + 1,
                    1),
                structure_config.deep
            ) AS ancestor_level(deep)
            WHERE structure_config.width > 0
              AND structure_config.height > 0
              AND ancestor.marketing_addr = structure_config.marketing_addr
              AND ancestor.structure_number = structure_config.structure_number
              AND ancestor.deep = ancestor_level.deep
              AND ancestor.mp = left(
                  structure_config.mp,
                  (ancestor_level.deep * 8)::integer);
            """, cancellationToken);
    }

    public void Add(Place place) => dataContext.Places.Add(place);

    public async Task RemoveRangeAsync(
        IReadOnlyCollection<Place> places,
        CancellationToken cancellationToken)
    {
        if (places.Count == 0)
            return;

        var ids = places.Select(place => place.Id).ToArray();
        var receipts = await dataContext.MarketingTasks
            .Where(task => ids.Contains(task.PlaceId)
                || ids.Contains(task.ResponseSourcePlaceId))
            .ToListAsync(cancellationToken);
        dataContext.MarketingTasks.RemoveRange(receipts);
        dataContext.Places.RemoveRange(places);
    }
}

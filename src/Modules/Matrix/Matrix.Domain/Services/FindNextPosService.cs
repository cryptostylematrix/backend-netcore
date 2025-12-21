namespace Matrix.Domain.Services;

public sealed class FindNextPosService(IPlaceReadOnlyRepository placeRepository, ILockReadOnlyRepository lockRepository) : 
    IFindNextPosService
{
    public async Task<Result<MultiPlace>> Find(short m, string profileAddr, CancellationToken cancellationToken = default)
    {
        // 1. Get root place
        var rootPlace = await placeRepository.GetRootPlace(m, profileAddr);
        if (rootPlace is null)
            return Result<MultiPlace>.NotFound();
        
        // 2. Load all locks for this profile (paged)
        var allLocks = new List<MultiLock>();
        var lockPage = 1;
        const int lockPageSize = 100;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var locksResult = await lockRepository.GetLocks(
                rootPlace.M,
                rootPlace.ProfileAddr,
                lockPage,
                lockPageSize);

            allLocks.AddRange(locksResult.Items);

            if (locksResult.Items.Count() < lockPageSize)
                break;

            lockPage++;
        }
        
        var lockMps = allLocks
            .Select(l => l.Mp)
            .ToArray();
           
        // 3. Walk open places (same algorithm as TS)
        var page = 1;
        const int pageSize = 50;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var openPlaces = await placeRepository.GetOpenPlacesByMpPrefix(
                rootPlace.M,
                rootPlace.Mp,
                page,
                pageSize);

            var ordered = openPlaces.Items
                .OrderBy(p => p.Mp.Length)
                .ThenBy(p => p.Mp, StringComparer.Ordinal);

            foreach (var place in ordered)
            {
                var childMp = place.Mp + place.Filling;

                if (IsLockedMp(childMp)) continue;

                return Result.Success(place);
            }

            if (openPlaces.Items.Count() < pageSize)
                return Result<MultiPlace>.NotFound();

            page++;
        }

        bool IsLockedMp(string mp)
        {
            return lockMps.Any(lockMp => mp.StartsWith(lockMp, StringComparison.Ordinal));
        }
    }
}
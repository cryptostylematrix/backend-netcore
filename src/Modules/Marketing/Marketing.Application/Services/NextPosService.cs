namespace Marketing.Application.Services;

public sealed class NextPosService(IPlaceQueries placeQueries, ILockQueries lockQueries) : INextPosService
{
    public async Task<NextPosResponse?> GetNextPosAsync(string marketingAddr, byte m, string profileAddr, CancellationToken ct)
    {
        // root
        var root = await placeQueries.GetRootPlaceAsync(
            marketingAddr: marketingAddr,
            m: m,
            profileAddr: profileAddr,
            ct);
        
        if (root is null)
            return null;

        // locks
        var lockMps = await lockQueries.GetAllLockMpsAsync(
            marketingAddr: root.MarketingAddr,
            m: root.M,
            profileAddr: root.ProfileAddr,
            ct);
        
        
        Array.Sort(lockMps, static (a, b) => a.Length.CompareTo(b.Length));

        // scan open places
        var page = 1;
        const int pageSize = 50;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var openPlaces = await placeQueries.GetOpenPlacesByMpPrefixAsync(
                marketingAddr: root.MarketingAddr,
                m: root.M,
                mpPrefix: root.Mp,
                page: page,
                pageSize: pageSize,
                ct);
                
            if (openPlaces.Count == 0)
                return null;

            // SQL already orders similarly, but keep if you want 1:1 behavior
            var ordered = openPlaces
                .OrderBy(p => p.Mp.Length)
                .ThenBy(p => p.Mp, StringComparer.Ordinal);

            foreach (var place in ordered)
            {
                var childPos = place.SeqNo + 1;
                var childMp = place.Mp + childPos.ToString("X8");;

                if (IsLockedMp(childMp, lockMps))
                    continue;

                return new NextPosResponse
                {
                    ParentAddr = place.Addr,
                    Pos = childPos,
                    Mp = childMp
                };
            }

            if (openPlaces.Count < pageSize)
                return null;

            page++;
        }
    }

    private static bool IsLockedMp(string mp, string[] lockMps)
    {
        return lockMps.Any(t => mp.StartsWith(t, StringComparison.Ordinal));
    }
}
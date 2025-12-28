namespace Matrix.Application.Services;

public sealed class NextPosService(IPlaceQueries placeQueries, ILockQueries lockQueries) : INextPosService
{
    public async Task<NextPosResponse?> GetNextPosAsync(short m, string profileAddr, CancellationToken ct)
    {
        // root
        var root = await placeQueries.GetRootPlaceAsync(m, profileAddr, ct);
        if (root is null)
            return null;

        // locks
        var lockMps = await lockQueries.GetAllLockMpsAsync(root.M, root.ProfileAddr, ct);
        Array.Sort(lockMps, static (a, b) => a.Length.CompareTo(b.Length));

        // scan open places
        var page = 1;
        const int pageSize = 50;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var openPlaces = await placeQueries.GetOpenPlacesByMpPrefixAsync(root.M, root.Mp, page, pageSize, ct);
            if (openPlaces.Count == 0)
                return null;

            // SQL already orders similarly, but keep if you want 1:1 behavior
            var ordered = openPlaces
                .OrderBy(p => p.Mp.Length)
                .ThenBy(p => p.Mp, StringComparer.Ordinal);

            foreach (var place in ordered)
            {
                var childMp = place.Mp + place.Filling;

                if (IsLockedMp(childMp, lockMps))
                    continue;

                return new NextPosResponse
                {
                    ParentAddr = place.Addr,
                    Pos = place.Filling,
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
namespace ReferalProgram.Application.Services.PositionStrategies;

public sealed class ClassicPositionAlgorithmStrategy(
    IPositionCandidateQueries placeQueries,
    IPositionLockQueries lockQueries) : IPositionAlgorithmStrategy
{
    public string Name => "classic";

    public async Task<NextPosResponse?> FindNextAsync(
        PositionAlgorithmStrategyContext context,
        CancellationToken cancellationToken)
    {
        var lockMps = await lockQueries.GetAllLockMpsAsync(
            context.Root.MarketingAddr,
            context.Root.StructNumber,
            context.Root.ProfileAddr,
            cancellationToken);

        Array.Sort(lockMps, static (left, right) =>
        {
            var lengthComparison = left.Length.CompareTo(right.Length);
            return lengthComparison != 0
                ? lengthComparison
                : string.CompareOrdinal(left, right);
        });

        var page = 1;
        const int pageSize = 50;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var openPlaces = await placeQueries.GetOpenPlacesByMpPrefixAsync(
                context.MarketingAddr,
                context.StructureNumber,
                context.Root.Mp,
                context.Width,
                page,
                pageSize,
                cancellationToken);

            if (openPlaces.Count == 0)
                return null;

            foreach (var place in openPlaces)
            {
                var pos = checked(place.Filling + 1);
                var childMp = place.Mp + pos.ToString("X8");

                if (IsLockedMp(childMp, lockMps))
                    continue;

                return new NextPosResponse
                {
                    ProfileAddr = place.ProfileAddr,
                    PlaceNumber = place.PlaceNumber,
                    Pos = pos,
                    Mp = childMp,
                    PosGroup = context.PosGroup
                };
            }

            if (openPlaces.Count < pageSize)
                return null;

            page++;
        }
    }

    private static bool IsLockedMp(string mp, string[] lockMps) =>
        lockMps.Any(lockMp => mp.StartsWith(lockMp, StringComparison.Ordinal));
}

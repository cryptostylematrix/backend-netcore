namespace ReferalProgram.Application.Services.PositionStrategies;

public sealed class EmptyParentPositionAlgorithmStrategy(
    IPositionCandidateQueries placeQueries) : IPositionAlgorithmStrategy
{
    public const string AlgorithmName = "empty_parent";

    public string Name => AlgorithmName;

    public async Task<NextPosResponse?> FindNextAsync(
        PositionAlgorithmStrategyContext context,
        CancellationToken cancellationToken)
    {
        var page = 1;
        const int pageSize = 50;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidates = await placeQueries.GetOpenPlacesByMpPrefixAsync(
                context.MarketingAddr,
                context.StructureNumber,
                context.Root.Mp,
                context.Width,
                page,
                pageSize,
                cancellationToken);
            var parent = candidates
                .Where(place => place.Filling == 0
                    && !context.IsLocked(place.Mp + "00000001"))
                .OrderBy(place => place.Deep)
                .ThenBy(place => place.Mp, StringComparer.Ordinal)
                .ThenBy(place => place.Id)
                .FirstOrDefault();
            if (parent is not null)
            {
                const uint pos = 1;
                return new NextPosResponse
                {
                    ProfileAddr = parent.ProfileAddr,
                    PlaceNumber = parent.PlaceNumber,
                    Pos = pos,
                    Mp = parent.Mp + pos.ToString("X8"),
                    PosGroup = context.PosGroup
                };
            }

            if (candidates.Count < pageSize)
                return null;

            page++;
        }
    }
}

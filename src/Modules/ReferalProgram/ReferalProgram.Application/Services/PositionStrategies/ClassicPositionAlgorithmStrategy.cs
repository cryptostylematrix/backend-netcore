namespace ReferalProgram.Application.Services.PositionStrategies;

public sealed class ClassicPositionAlgorithmStrategy(
    IPositionCandidateQueries placeQueries) : IPositionAlgorithmStrategy
{
    public string Name => "classic";

    public async Task<NextPosResponse?> FindNextAsync(
        PositionAlgorithmStrategyContext context,
        CancellationToken cancellationToken) =>
        await FindClassicNextAsync(placeQueries, context, cancellationToken);

    internal static async Task<NextPosResponse?> FindClassicNextAsync(
        IPositionCandidateQueries placeQueries,
        PositionAlgorithmStrategyContext context,
        CancellationToken cancellationToken)
    {
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

                if (context.IsLocked(childMp))
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
}

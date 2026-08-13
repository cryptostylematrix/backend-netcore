namespace ReferalProgram.Application.Services.PositionStrategies;

public sealed class RadarPositionAlgorithmStrategy(IPositionCandidateQueries placeQueries)
    : IPositionAlgorithmStrategy
{
    public string Name => "radar";

    public async Task<NextPosResponse?> FindNextAsync(
        PositionAlgorithmStrategyContext context,
        CancellationToken cancellationToken)
    {
        if (context.Width == 0)
            return null;

        if (context.DepthSpread == 0)
            throw new InvalidOperationException("Radar requires a positive depth spread.");

        var place = await placeQueries.GetFirstActiveUnfilledPlaceAsync(
            context.MarketingAddr,
            context.StructureNumber,
            context.Root.Mp,
            context.Width,
            context.ProfiledPlacesPrioritized,
            context.DepthSpread,
            cancellationToken);

        if (place is null)
            return null;

        var pos = checked(place.Filling + 1);
        return new NextPosResponse
        {
            ProfileAddr = place.ProfileAddr,
            PlaceNumber = place.PlaceNumber,
            Pos = pos,
            Mp = place.Mp + pos.ToString("X8"),
            PosGroup = context.PosGroup
        };
    }
}

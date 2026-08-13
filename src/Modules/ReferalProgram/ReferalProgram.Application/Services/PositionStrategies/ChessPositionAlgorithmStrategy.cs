namespace ReferalProgram.Application.Services.PositionStrategies;

public sealed class ChessPositionAlgorithmStrategy(IPositionCandidateQueries placeQueries)
    : IPositionAlgorithmStrategy
{
    public string Name => "chess";

    public async Task<NextPosResponse?> FindNextAsync(
        PositionAlgorithmStrategyContext context,
        CancellationToken cancellationToken)
    {
        if (context.Width == 0)
            return null;

        if (context.DepthSpread == 0)
            throw new InvalidOperationException("Chess requires a positive depth spread.");

        var places = await placeQueries.GetUnfilledPlacesInDepthWindowAsync(
            context.MarketingAddr,
            context.StructureNumber,
            context.Root.Mp,
            context.Width,
            context.DepthSpread,
            cancellationToken);

        IReadOnlyList<PlaceResponse>[] placeGroups = context.ProfiledPlacesPrioritized
            ?
            [
                places.Where(place => !string.IsNullOrWhiteSpace(place.ProfileAddr)).ToArray(),
                places.Where(place => string.IsNullOrWhiteSpace(place.ProfileAddr)).ToArray()
            ]
            : [places];

        foreach (var placeGroup in placeGroups)
        {
            for (uint filling = 0; filling < context.Width; filling++)
            {
                foreach (var place in ChessOrder(placeGroup))
                {
                    if (place.Filling != filling)
                        continue;

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
        }

        return null;
    }

    private static IEnumerable<PlaceResponse> ChessOrder(
        IReadOnlyList<PlaceResponse> places)
    {
        var left = 0;
        var right = places.Count - 1;

        while (left <= right)
        {
            yield return places[left++];

            if (left <= right)
                yield return places[right--];
        }
    }
}

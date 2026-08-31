namespace ReferalProgram.Application.Services.PositionStrategies;

public sealed class ProfileFrontierPositionAlgorithmStrategy(
    IPositionCandidateQueries placeQueries) : IPositionAlgorithmStrategy
{
    public const string AlgorithmName = "profile_frontier";

    public string Name => AlgorithmName;

    public async Task<NextPosResponse?> FindNextAsync(
        PositionAlgorithmStrategyContext context,
        CancellationToken cancellationToken)
    {
        if (context.ProfiledFrontierLimit is null or 0)
        {
            throw new InvalidOperationException(
                "Profile frontier positioning requires a positive profiled frontier limit.");
        }

        var parent = await placeQueries.GetProfileFrontierCandidateAsync(
            context.MarketingAddr,
            context.StructureNumber,
            context.Root.Mp,
            context.Width,
            context.ProfiledFrontierLimit.Value,
            context.RootProfileLockMps,
            cancellationToken);

        return parent is null ? null : BuildPosition(parent, context.PosGroup);
    }

    private static NextPosResponse BuildPosition(PlaceResponse parent, byte posGroup)
    {
        var pos = checked(parent.Filling + 1);
        return new NextPosResponse
        {
            ProfileAddr = parent.ProfileAddr,
            PlaceNumber = parent.PlaceNumber,
            Pos = pos,
            Mp = parent.Mp + pos.ToString("X8"),
            PosGroup = posGroup
        };
    }
}

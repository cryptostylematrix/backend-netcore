namespace ReferalProgram.Application.Services.PositionStrategies;

public sealed class SystemGapPositionAlgorithmStrategy(
    IPositionCandidateQueries placeQueries) : IPositionAlgorithmStrategy
{
    public const string AlgorithmName = "system_gap";

    public string Name => AlgorithmName;

    public async Task<NextPosResponse?> FindNextAsync(
        PositionAlgorithmStrategyContext context,
        CancellationToken cancellationToken)
    {
        var parent = await placeQueries.GetSystemGapCandidateAsync(
            context.MarketingAddr,
            context.StructureNumber,
            context.Root.Mp,
            context.Width,
            context.RootProfileLockMps,
            cancellationToken);

        if (parent is null)
            return null;

        var pos = checked(parent.Filling + 1);
        return new NextPosResponse
        {
            ProfileAddr = parent.ProfileAddr,
            PlaceNumber = parent.PlaceNumber,
            Pos = pos,
            Mp = parent.Mp + pos.ToString("X8"),
            PosGroup = context.PosGroup
        };
    }
}

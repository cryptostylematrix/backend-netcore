namespace ReferalProgram.Application.Services;

public sealed class NextPosService(
    INextPositionQueries queries,
    IPositionAlgorithmConfigurationParser configurationParser,
    IPositionGroupSelector groupSelector,
    IPositionRootResolver positionRootResolver,
    IPositionAlgorithmResolver algorithmResolver) : INextPosService
{
    public async Task<NextPosResponse?> GetNextPosAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken ct)
    {
        var structure = await queries.GetStructureAsync(
            marketingAddr,
            structureNumber,
            ct);

        if (structure is null)
            return null;

        var config = configurationParser.Parse(structure.PosAlgo);

        var counts = await queries.GetPlaceCountsByPosGroupAsync(
            marketingAddr,
            structureNumber,
            ct);

        var group = groupSelector.Select(config, counts);

        var root = await positionRootResolver.ResolveAsync(
            config.Root,
            marketingAddr,
            structureNumber,
            profileAddr,
            ct);

        if (root is null)
            return null;

        var positionStrategy = algorithmResolver.Resolve(group.Algorithm);

        return await positionStrategy.FindNextAsync(
            new PositionAlgorithmStrategyContext(
                marketingAddr,
                structureNumber,
                structure.Width,
                root,
                checked((byte)group.Id),
                group.ProfiledPlacesPrioritized,
                group.DepthSpread),
            ct);
    }
}

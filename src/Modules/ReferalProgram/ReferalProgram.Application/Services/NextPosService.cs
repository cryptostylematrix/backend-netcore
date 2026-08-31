namespace ReferalProgram.Application.Services;

public sealed class NextPosService(
    INextPositionQueries queries,
    IPositionAlgorithmConfigurationParser configurationParser,
    IPositionGroupSelector groupSelector,
    IPositionRootResolver positionRootResolver,
    IPositionAlgorithmResolver algorithmResolver,
    IPositionLockQueries lockQueries) : INextPosService
{
    public async Task<NextPosResponse?> GetNextPosAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        PositionOperation? operation,
        CancellationToken ct)
    {
        var selection = await ResolveSelectionAsync(
            marketingAddr,
            structureNumber,
            profileAddr,
            operation,
            ct);

        return selection is null
            ? null
            : await FindNextAsync(selection, ct);
    }

    public async Task<PositionSelection?> ResolveSelectionAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        PositionOperation? operation,
        CancellationToken ct)
    {
        var structure = await queries.GetStructureAsync(
            marketingAddr,
            structureNumber,
            ct);

        if (structure is null)
            return null;

        var config = configurationParser.Parse(structure.PosAlgo, operation);

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

        var lockMps = await lockQueries.GetAllLockMpsAsync(
            marketingAddr,
            structureNumber,
            root.ProfileAddr,
            ct);

        return new PositionSelection(
            group.Algorithm,
            new PositionAlgorithmStrategyContext(
                marketingAddr,
                structureNumber,
                structure.Width,
                root,
                checked((byte)group.Id),
                group.ProfiledPlacesPrioritized,
                group.DepthSpread,
                lockMps,
                group.CutFactor,
                group.ProfiledFrontierLimit));
    }

    public Task<NextPosResponse?> FindNextAsync(
        PositionSelection selection,
        CancellationToken ct)
    {
        var positionStrategy = algorithmResolver.Resolve(selection.Algorithm);

        return positionStrategy.FindNextAsync(selection.Context, ct);
    }
}

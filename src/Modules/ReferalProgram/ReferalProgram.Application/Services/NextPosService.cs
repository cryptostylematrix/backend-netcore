using System.Text.Json;

namespace ReferalProgram.Application.Services;

public sealed class NextPosService(
    INextPositionQueries queries,
    IPositionAlgorithmConfigurationParser configurationParser,
    IPositionGroupSelector groupSelector,
    IPositionRootResolver positionRootResolver,
    IPositionAlgorithmResolver algorithmResolver,
    IPositionLockQueries lockQueries,
    IPlaceQueries placeQueries) : INextPosService
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

        PlacementActivityRules? activityRules = null;
        // Legacy activity JSON only governed activation, never placement.
        if (structureNumber > 0 && structure.Activity is { ValueKind: JsonValueKind.Object } activity
            && (activity.TryGetProperty("type", out _)
                || activity.TryGetProperty("when_inactive", out _)
                || activity.TryGetProperty("spillover", out _)
                || activity.TryGetProperty("preserve_status_on_activation", out _)))
        {
            var settings = (MarketingActivitySettings)ActivitySettings.Parse(activity, structureNumber);
            var rules = settings.WhenInactive;
            var spillover = settings.Spillover;
            if (rules.AllowOwnChildren || rules.CheckManualPlacement
                || spillover.AllowInactivePlace || spillover.RequireActiveInvite)
            {
                var invite = string.IsNullOrWhiteSpace(profileAddr) ? null
                    : await placeQueries.GetPlaceAsync(marketingAddr, 0, profileAddr, 1, ct);
                activityRules = new PlacementActivityRules(profileAddr, invite?.ParentProfileAddr,
                    rules.AllowOwnChildren, spillover.AllowInactivePlace,
                    spillover.RequireActiveInvite, rules.CheckManualPlacement);
            }
        }

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
                group.EffectiveProfiledWidthLimit,
                activityRules));
    }

    public Task<NextPosResponse?> FindNextAsync(
        PositionSelection selection,
        CancellationToken ct)
    {
        var positionStrategy = algorithmResolver.Resolve(selection.Algorithm);

        return positionStrategy.FindNextAsync(selection.Context, ct);
    }
}

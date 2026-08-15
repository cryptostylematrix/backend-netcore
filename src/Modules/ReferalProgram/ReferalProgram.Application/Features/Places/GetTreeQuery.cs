using Contracts.Application.Features.ProfileItem;
using MediatR;

namespace ReferalProgram.Application.Features.Places;

public sealed record GetTreeQuery(
    string MarketingAddr,
    byte StructureNumber,
    string? ProfileAddr,
    uint PlaceNumber,
    string ViewerProfileAddr,
    string? ViewerWalletAddr,
    uint FromPos,
    uint ToPos) : IQuery<TreeNodeResponse>;

internal sealed class GetTreeQueryHandler(
    IPlaceQueries placeQueries,
    ILockQueries lockQueries,
    IStructureQueries structureQueries,
    IPositionRootResolver positionRootResolver,
    IPositionAlgorithmConfigurationParser configurationParser,
    INextPosService nextPosService,
    IBuyPlacePolicy buyPlacePolicy,
    IProgramCommandQueries commandQueries,
    IPositionNodeActionPolicy actionPolicy,
    ITonAddressComparer addressComparer,
    ISender sender)
    : IQueryHandler<GetTreeQuery, TreeNodeResponse>
{
    public async Task<Result<TreeNodeResponse>> Handle(
        GetTreeQuery request,
        CancellationToken ct)
    {
        if (request.FromPos > request.ToPos)
            return Result<TreeNodeResponse>.Error("FromPos must be less than or equal to ToPos.");

        if (string.IsNullOrWhiteSpace(request.ViewerProfileAddr))
            return Result<TreeNodeResponse>.Error("ViewerProfileAddr is required.");

        if (string.IsNullOrWhiteSpace(request.ViewerWalletAddr))
            return Result<TreeNodeResponse>.Error("ViewerWalletAddr is required.");

        var structure = await structureQueries.GetStructureAsync(
            request.MarketingAddr,
            request.StructureNumber,
            ct);

        if (structure is null)
            return Result<TreeNodeResponse>.NotFound();

        if (request.FromPos < 1
            || (structure.Width > 0 && request.ToPos > structure.Width))
            return Result<TreeNodeResponse>.Error(
                $"Positions must be between 1 and structure width {structure.Width}.");

        var profileAddr = string.IsNullOrWhiteSpace(request.ProfileAddr)
            ? null
            : request.ProfileAddr;

        var selected = await placeQueries.GetPlaceAsync(
            request.MarketingAddr,
            request.StructureNumber,
            profileAddr,
            request.PlaceNumber,
            ct);

        if (selected is null)
            return Result<TreeNodeResponse>.NotFound();

        PlaceResponse? selectedParent = null;
        if (selected.ParentPlaceNumber is not null)
        {
            selectedParent = await placeQueries.GetPlaceAsync(
                request.MarketingAddr,
                request.StructureNumber,
                selected.ParentProfileAddr,
                selected.ParentPlaceNumber.Value,
                ct);
        }

        var configuration = configurationParser.Parse(structure.PosAlgo);

        var viewerRoot = await positionRootResolver.ResolveAsync(
            configuration.Root,
            request.MarketingAddr,
            request.StructureNumber,
            request.ViewerProfileAddr,
            ct);

        if (viewerRoot is null)
            return Result<TreeNodeResponse>.NotFound();

        var buyDecision = await buyPlacePolicy.EvaluateAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.ViewerProfileAddr,
            requestedPosition: null,
            ct);

        // The next position describes the structure, not the viewer's permission
        // to buy. A denied purchase (for example, max places reached) must not
        // hide the position from the tree.
        var nextPosition = buyDecision.Position
            ?? await nextPosService.GetNextPosAsync(
                request.MarketingAddr,
                request.StructureNumber,
                request.ViewerProfileAddr,
                ct);

        var lockMps = await lockQueries.GetAllLockMpsAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.ViewerProfileAddr,
            ct);
        var commandConfiguration = await commandQueries.GetConfigurationAsync(
            request.MarketingAddr,
            ct);
        var availableCommandTags = commandConfiguration.GetAvailableCommandTags(
            request.StructureNumber);

        var profileResult = await sender.Send(
            new GetNftDataQuery(request.ViewerProfileAddr),
            ct);
        var viewerOwnsProfile = profileResult.IsSuccess
            && addressComparer.AreEqual(
                profileResult.Value.OwnerAddr,
                request.ViewerWalletAddr);

        var actionContext = new PositionNodeActionContext(
            structure.Width,
            viewerRoot.Mp,
            lockMps,
            viewerOwnsProfile,
            availableCommandTags,
            buyDecision,
            buyDecision.IncludePosition);

        var subtree = await placeQueries.GetPlacesByMpPrefixAsync(
            request.MarketingAddr,
            request.StructureNumber,
            selected.Mp,
            structure.DisplayHeight,
            request.FromPos,
            request.ToPos,
            ct);

        var rowsByMp = subtree
            .GroupBy(place => place.Mp, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        rowsByMp[selected.Mp] = selected;

        return Result.Success(await BuildNode(
            selected,
            selectedParent,
            selected.Mp,
            selected.Pos,
            structure.DisplayHeight));

        async Task<TreeNodeResponse> BuildNode(
            PlaceResponse? row,
            PlaceResponse? parent,
            string mp,
            uint pos,
            byte depthRemaining)
        {
            TreeNodeResponse[]? children = null;

            if (depthRemaining > 0)
            {
                var childNodes = new List<TreeNodeResponse>();
                for (var childPos = request.FromPos; childPos <= request.ToPos; childPos++)
                {
                    var childMp = mp + childPos.ToString("X8");
                    rowsByMp.TryGetValue(childMp, out var childRow);
                    childNodes.Add(await BuildNode(
                        childRow,
                        row,
                        childMp,
                        childPos,
                        checked((byte)(depthRemaining - 1))));

                    if (childPos == uint.MaxValue)
                        break;
                }

                children = childNodes.ToArray();
            }

            if (row is null)
            {
                var actions = actionPolicy.Evaluate(actionContext, parent, mp, pos);
                var isNextPos = string.Equals(
                    mp,
                    nextPosition?.Mp,
                    StringComparison.Ordinal);

                return new TreeEmptyNodeResponse
                {
                    Locked = actions.IsLocked,
                    IsLock = actions.IsLock,
                    ParentProfileAddr = parent?.ProfileAddr,
                    ParentPlaceNumber = parent?.PlaceNumber,
                    Pos = pos,
                    Width = structure.Width,
                    Height = structure.DisplayHeight,
                    Children = children,
                    IsNextPos = isNextPos,
                    CanBuy = actions.CanBuy,
                    CanLock = actions.CanLock,
                    CanUnlock = actions.CanUnlock,
                    BuyCommandTag = actions.BuyCommandTag,
                    IncludePosition = actions.IncludePosition
                };
            }

            var filledActions = actionPolicy.Evaluate(actionContext, parent, mp, pos);
            var subtreeCounts = await placeQueries.GetPlaceSubtreeCountsAsync(
                request.MarketingAddr,
                request.StructureNumber,
                row.Mp,
                structure.Height,
                ct);
            return new TreeFilledNodeResponse
            {
                Locked = filledActions.IsLocked,
                IsLock = filledActions.IsLock,
                CanLock = filledActions.CanLock,
                CanUnlock = filledActions.CanUnlock,
                ParentProfileAddr = row.ParentProfileAddr,
                ParentPlaceNumber = row.ParentPlaceNumber,
                Pos = row.Pos,
                Width = structure.Width,
                Height = structure.DisplayHeight,
                Children = children,
                PlaceNumber = row.PlaceNumber,
                ProfileAddr = row.ProfileAddr,
                ProfileLogin = row.ProfileLogin,
                Kind = row.Kind,
                Filling = row.Filling,
                MatrixPlacesCount = subtreeCounts.MatrixPlacesCount,
                Descendants = subtreeCounts.DescendantsCount,
                Level = row.Deep,
                IsActive = row.IsActive,
                CreatedAt = row.CreatedAt,
                ActivatedAt = row.ActivatedAt,
                IsRoot = string.Equals(
                    row.Mp,
                    viewerRoot.Mp,
                    StringComparison.Ordinal)
            };
        }

    }
}

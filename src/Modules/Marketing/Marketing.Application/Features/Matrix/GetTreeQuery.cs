namespace Marketing.Application.Features.Matrix;

public sealed record GetTreeQuery(
    string MarketingAddr,
    string ProfileAddr,
    string PlaceAddr,
    uint FromPos,
    uint ToPos) : IQuery<TreeNodeResponse>;

internal sealed class GetTreeQueryHandler(
    IPlaceQueries placeQueries,
    ILockQueries lockQueries,
    INextPosService nextPosService)
    : IQueryHandler<GetTreeQuery, TreeNodeResponse>
{
    public async Task<Result<TreeNodeResponse>> Handle(GetTreeQuery request, CancellationToken ct)
    {
        if (request.FromPos > request.ToPos)
            return Result<TreeNodeResponse>.Error("FromPos must be less than ToPos");

        var selected = await placeQueries.GetPlaceByAddressAsync(
            marketingAddr: request.MarketingAddr,
            placeAddr: request.PlaceAddr,
            ct);

        if (selected is null)
            return Result<TreeNodeResponse>.NotFound();

        var root = await placeQueries.GetRootPlaceAsync(
            marketingAddr: request.MarketingAddr,
            m: selected.M,
            profileAddr: request.ProfileAddr,
            ct);

        if (root is null)
            return Result<TreeNodeResponse>.NotFound();

        var lockMps = await lockQueries.GetAllLockMpsAsync(
            marketingAddr: request.MarketingAddr,
            m: root.M,
            profileAddr: root.ProfileAddr,
            ct);

        var nextPos = await nextPosService.GetNextPosAsync(
            marketingAddr: request.MarketingAddr,
            m: root.M,
            profileAddr: root.ProfileAddr,
            ct);

        if (nextPos is null)
            return Result<TreeNodeResponse>.NotFound();

        var treeInfo = new TreeInfo(root, nextPos.Mp, lockMps);

        PlaceResponse? parentOfSelected = null;

        if (selected.ParentAddr is not null)
        {
            parentOfSelected = await placeQueries.GetPlaceByAddressAsync(
                marketingAddr: request.MarketingAddr,
                placeAddr: selected.ParentAddr,
                ct);
        }

        var subtree = await placeQueries.GetPlacesByMpPrefixAsync(
            marketingAddr: selected.MarketingAddr,
            m: selected.M,
            mpPrefix: selected.Mp,
            depthLevels: selected.Height,
            fromPos: request.FromPos,
            toPos: request.ToPos,
            ct: ct);

        var rowsByMp = subtree.ToDictionary(x => x.Mp);

        rowsByMp[selected.Mp] = selected;

        var rootTreeNode = await BuildTreeNodeAsync(
            row: selected,
            parentRow: parentOfSelected,
            mp: selected.Mp,
            depthRemaining: selected.Height,
            ct: ct);

        return Result<TreeNodeResponse>.Success(rootTreeNode);

        async Task<TreeNodeResponse> BuildTreeNodeAsync(
            PlaceResponse? row,
            PlaceResponse? parentRow,
            string mp,
            int depthRemaining,
            CancellationToken ct)
        {
            var npi = treeInfo.GetNodePosInfo(parentRow, mp);

            TreeNodeResponse[]? children = null;

            if (depthRemaining > 0)
            {
                var childNodes = new List<TreeNodeResponse>();

                for (var pos = request.FromPos; pos <= request.ToPos; pos++)
                {
                    var childMp = mp + pos.ToString("X8");

                    rowsByMp.TryGetValue(childMp, out var childRow);

                    var childNode = await BuildTreeNodeAsync(
                        row: childRow,
                        parentRow: row,
                        mp: childMp,
                        depthRemaining: depthRemaining - 1,
                        ct: ct);

                    childNodes.Add(childNode);

                    if (pos == uint.MaxValue)
                        break;
                }

                children = childNodes.ToArray();
            }

            if (row is null)
                return BuildEmptyTreeNode(npi, parentRow, children);

            return await BuildFilledTreeNode(npi, row, children, ct);
        }

        TreeEmptyNodeResponse BuildEmptyTreeNode(
            NodePosInfo npi,
            PlaceResponse? parentRow,
            TreeNodeResponse[]? children)
        {
            var canLock = npi.CanLock;

            if (npi.Pos == request.FromPos)
                canLock = false;

            return new TreeEmptyNodeResponse
            {
                Locked = npi.IsLocked,
                CanLock = canLock,
                IsLock = npi.IsLock,
                Pos = npi.Pos,
                IsNextPos = npi.IsNextPos,
                CanBuy = npi.CanBuy,
                ParentAddr = parentRow?.Addr,
                Children = children
            };
        }

        async Task<TreeFilledNodeResponse> BuildFilledTreeNode(
            NodePosInfo npi,
            PlaceResponse placeRow,
            TreeNodeResponse[]? children,
            CancellationToken token)
        {
            var count = await placeQueries.GetPlacesCountByMpPrefixAsync(
                marketingAddr: placeRow.MarketingAddr,
                m: placeRow.M,
                mpPrefix: placeRow.Mp,
                token);

            var descendants = Math.Max(0, count - 1);

            return new TreeFilledNodeResponse
            {
                Addr = placeRow.Addr,
                CanLock = npi.CanLock,
                Locked = npi.IsLocked,
                IsLock = npi.IsLock,
                Pos = placeRow.Pos,
                SeqNo =  placeRow.SeqNo,
                Width = placeRow.Width,
                Height = placeRow.Height,
                IsRoot = npi.IsRoot,

                PlaceNumber = placeRow.PlaceNumber,
                ParentAddr = placeRow.ParentAddr,
                Kind = placeRow.Kind,
                CreatedAt = placeRow.CreatedAt,
                ProfileLogin = placeRow.ProfileLogin,
                ProfileAddr = placeRow.ProfileAddr,

                Descendants = descendants,
                Children = children
            };
        }
    }
}
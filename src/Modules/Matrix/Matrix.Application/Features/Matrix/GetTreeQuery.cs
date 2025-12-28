namespace Matrix.Application.Features.Matrix;

public sealed record GetTreeQuery(string ProfileAddr, string PlaceAddr) : IQuery<TreeNodeResponse>;

internal sealed class GetTreeQueryHandler(
    IPlaceQueries placeQueries, 
    ILockQueries lockQueries,
    INextPosService nextPosService) : IQueryHandler<GetTreeQuery, TreeNodeResponse>
{
    public async Task<Result<TreeNodeResponse>> Handle(GetTreeQuery request, CancellationToken ct)
    {
        // 1) Selected node by address
        var selected = await placeQueries.GetPlaceByAddressAsync(request.PlaceAddr, ct);
        if (selected is null)
            return Result<TreeNodeResponse>.NotFound();

        // 2) Root node for this profile
        var root = await placeQueries.GetRootPlaceAsync(selected.M, request.ProfileAddr, ct);
        if (root is null)
            return Result<TreeNodeResponse>.NotFound();

        // 3) Load lock MPs
        var lockMps = await lockQueries.GetAllLockMpsAsync(root.M, root.ProfileAddr, ct);

        // 4) Compute nextMp (same algorithm as GetNextPosQueryHandler, but inline + using queries)
        var nextPos = await nextPosService.GetNextPosAsync(root.M, root.ProfileAddr, ct);
        if (nextPos is null)
            return Result<TreeNodeResponse>.NotFound();
        
        var nextMp = nextPos.Mp;

        // 5) Load subtree (depth 2)
        var subtree = await placeQueries.GetPlacesByMpPrefixAsync(
            m: selected.M,
            mpPrefix: selected.Mp,
            depthLevels: 2,
            page: 1,
            pageSize: 7,
            ct);

        var leftRow  = subtree.FirstOrDefault(p => p.ParentId == selected.Id && p.Pos == 0);
        var rightRow = subtree.FirstOrDefault(p => p.ParentId == selected.Id && p.Pos == 1);

        var treeInfo = new TreeInfo(root, nextMp, lockMps);

        var npiSelected   = treeInfo.GetNodePosInfo(null, selected.Mp);

        var npiLeft       = treeInfo.GetNodePosInfo(selected, $"{selected.Mp}0");
        var npiLeftLeft   = treeInfo.GetNodePosInfo(leftRow,   $"{selected.Mp}00");
        var npiLeftRight  = treeInfo.GetNodePosInfo(leftRow,   $"{selected.Mp}01");

        var npiRight      = treeInfo.GetNodePosInfo(selected, $"{selected.Mp}1");
        var npiRightLeft  = treeInfo.GetNodePosInfo(rightRow,  $"{selected.Mp}10");
        var npiRightRight = treeInfo.GetNodePosInfo(rightRow,  $"{selected.Mp}11");

        // ----- left side -----
        TreeNodeResponse leftNode;
        TreeNodeResponse leftLeftNode;
        TreeNodeResponse leftRightNode;

        if (leftRow is null)
        {
            leftLeftNode  = BuildEmptyTreeNode(npiLeftLeft,  null, null);
            leftRightNode = BuildEmptyTreeNode(npiLeftRight, null, null);
            leftNode      = BuildEmptyTreeNode(npiLeft, selected, [leftLeftNode, leftRightNode]);
        }
        else
        {
            var leftLeftRow  = subtree.FirstOrDefault(p => p.ParentId == leftRow.Id && p.Pos == 0);
            var leftRightRow = subtree.FirstOrDefault(p => p.ParentId == leftRow.Id && p.Pos == 1);

            leftLeftNode = leftLeftRow is not null
                ? await BuildFilledTreeNode(npiLeftLeft, leftLeftRow, null, ct)
                : BuildEmptyTreeNode(npiLeftLeft, leftRow, null);

            leftRightNode = leftRightRow is not null
                ? await BuildFilledTreeNode(npiLeftRight, leftRightRow, null, ct)
                : BuildEmptyTreeNode(npiLeftRight, leftRow, null);

            leftNode = await BuildFilledTreeNode(npiLeft, leftRow, [leftLeftNode, leftRightNode], ct);
        }

        // ----- right side -----
        TreeNodeResponse rightNode;
        TreeNodeResponse rightLeftNode;
        TreeNodeResponse rightRightNode;

        if (rightRow is null)
        {
            rightLeftNode  = BuildEmptyTreeNode(npiRightLeft,  null, null);
            rightRightNode = BuildEmptyTreeNode(npiRightRight, null, null);
            rightNode      = BuildEmptyTreeNode(npiRight, selected, [rightLeftNode, rightRightNode]);
        }
        else
        {
            var rightLeftRow  = subtree.FirstOrDefault(p => p.ParentId == rightRow.Id && p.Pos == 0);
            var rightRightRow = subtree.FirstOrDefault(p => p.ParentId == rightRow.Id && p.Pos == 1);

            rightLeftNode = rightLeftRow is not null
                ? await BuildFilledTreeNode(npiRightLeft, rightLeftRow, null, ct)
                : BuildEmptyTreeNode(npiRightLeft, rightRow, null);

            rightRightNode = rightRightRow is not null
                ? await BuildFilledTreeNode(npiRightRight, rightRightRow, null, ct)
                : BuildEmptyTreeNode(npiRightRight, rightRow, null);

            rightNode = await BuildFilledTreeNode(npiRight, rightRow, [rightLeftNode, rightRightNode], ct);
        }

        var rootTreeNode = await BuildFilledTreeNode(npiSelected, selected, [leftNode, rightNode], ct);
        return Result<TreeNodeResponse>.Success(rootTreeNode);

        // ---------------- local builders ----------------

        TreeEmptyNodeResponse BuildEmptyTreeNode(NodePosInfo npi, PlaceResponse? parentRow, TreeNodeResponse[]? children)
        {
            var canLock = npi.CanLock;

            // your original rule: cannot lock left place if it's empty because we build from left to right
            if (npi.Pos == 0)
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
            var count = await placeQueries.GetPlacesCountByMpPrefixAsync(placeRow.M, placeRow.Mp, token);
            var descendants = Math.Max(0, count - 1);

            return new TreeFilledNodeResponse
            {
                Addr = placeRow.Addr,
                CanLock = npi.CanLock,
                Locked = npi.IsLocked,
                IsLock = npi.IsLock,
                Pos = npi.Pos,
                IsRoot = npi.IsRoot,

                PlaceNumber = placeRow.PlaceNumber,
                ParentAddr = placeRow.ParentAddr,
                Clone = placeRow.Clone,
                CreatedAt = placeRow.CreatedAt,
                ProfileLogin = placeRow.ProfileLogin,
                ProfileAddr = placeRow.ProfileAddr,

                Descendants = descendants,
                Children = children
            };
        }
    }
}

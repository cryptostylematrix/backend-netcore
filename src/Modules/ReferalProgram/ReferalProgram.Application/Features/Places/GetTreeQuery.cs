namespace ReferalProgram.Application.Features.Places;

public sealed record GetTreeQuery(
    string MarketingAddr,
    byte StructureNumber,
    string? ProfileAddr,
    uint PlaceNumber,
    uint FromPos,
    uint ToPos) : IQuery<TreeNodeResponse>;

internal sealed class GetTreeQueryHandler(
    IPlaceQueries placeQueries,
    IStructureQueries structureQueries,
    INextPosService nextPosService)
    : IQueryHandler<GetTreeQuery, TreeNodeResponse>
{
    public async Task<Result<TreeNodeResponse>> Handle(
        GetTreeQuery request,
        CancellationToken ct)
    {
        if (request.FromPos > request.ToPos)
            return Result<TreeNodeResponse>.Error("FromPos must be less than or equal to ToPos.");

        var structure = await structureQueries.GetStructureAsync(
            request.MarketingAddr,
            request.StructureNumber,
            ct);

        if (structure is null)
            return Result<TreeNodeResponse>.NotFound();

        if (request.FromPos < 1 || request.ToPos > structure.Width)
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

        var nextPosition = await nextPosService.GetNextPosAsync(
            request.MarketingAddr,
            request.StructureNumber,
            profileAddr,
            ct);

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

        return Result.Success(BuildNode(
            selected,
            parent: null,
            selected.Mp,
            selected.Pos,
            structure.DisplayHeight));

        TreeNodeResponse BuildNode(
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
                    childNodes.Add(BuildNode(
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
                var isNextPos = string.Equals(
                    mp,
                    nextPosition?.Mp,
                    StringComparison.Ordinal);
                var canBuy = parent is not null
                    && mp.StartsWith(selected.Mp, StringComparison.Ordinal)
                    && pos == checked(parent.Filling + 1);

                return new TreeEmptyNodeResponse
                {
                    ParentProfileAddr = parent?.ProfileAddr,
                    ParentPlaceNumber = parent?.PlaceNumber,
                    Pos = pos,
                    Width = structure.Width,
                    Height = structure.DisplayHeight,
                    Children = children,
                    IsNextPos = isNextPos,
                    CanBuy = canBuy
                };
            }

            return new TreeFilledNodeResponse
            {
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
                Level = row.Deep,
                IsActive = row.IsActive,
                CreatedAt = row.CreatedAt,
                ActivatedAt = row.ActivatedAt,
                IsRoot = row.ParentId is null
            };
        }
    }
}

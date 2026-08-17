namespace ReferalProgram.Application.Features.StructureRanks;

public sealed record GetStructureRanksQuery(
    string MarketingAddr,
    byte StructureNumber) : IQuery<IReadOnlyCollection<StructureRankResponse>>;

internal sealed class GetStructureRanksQueryHandler(IStructureRankQueries rankQueries)
    : IQueryHandler<GetStructureRanksQuery, IReadOnlyCollection<StructureRankResponse>>
{
    public async Task<Result<IReadOnlyCollection<StructureRankResponse>>> Handle(
        GetStructureRanksQuery request,
        CancellationToken cancellationToken)
    {
        var ranks = await rankQueries.GetAllAsync(
            request.MarketingAddr,
            request.StructureNumber,
            cancellationToken);

        return Result.Success(ranks);
    }
}

namespace Contracts.Application.Features.Marketing;

public sealed record BuildBuyPlaceByTonBodyQuery(
    int M, 
    string ProfileAddr, 
    bool First,
    string? ParentAddr, 
    int? Pos) : IQuery<BuyPlaceByTonBodyResponse>;
    
       
internal sealed class BuildBuyPlaceByTonBodyQueryHandler(IMarketingQueries queries)
    : IQueryHandler<BuildBuyPlaceByTonBodyQuery, BuyPlaceByTonBodyResponse>
{
    public Task<Result<BuyPlaceByTonBodyResponse>> Handle(BuildBuyPlaceByTonBodyQuery request, CancellationToken ct)
        => Task.FromResult(queries.BuildBuyPlaceByTonBody(
            queryId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            m: request.M,
            profileAddr: request.ProfileAddr,
            first: request.First,
            parentAddr: request.ParentAddr,
            pos: request.Pos));
}
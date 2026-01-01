namespace Contracts.Application.Features.Multi;

public sealed record BuildBuyPlaceBodyQuery(
    int M, 
    string ProfileAddr, 
    string? ParentAddr, 
    int? Pos) : IQuery<BuyPlaceBodyResponse>;
    
       
internal sealed class BuildBuyPlaceBodyQueryHandler(IMultiQueries queries)
    : IQueryHandler<BuildBuyPlaceBodyQuery, BuyPlaceBodyResponse>
{
    public Task<Result<BuyPlaceBodyResponse>> Handle(BuildBuyPlaceBodyQuery request, CancellationToken ct)
        => Task.FromResult(queries.BuildBuyPlaceBody(
            queryId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            m: request.M,
            profileAddr: request.ProfileAddr,
            parentAddr: request.ParentAddr,
            pos: request.Pos));
}
namespace Contracts.Application.Features.Marketing;


public sealed record BuildBuyPlaceByJettonBodyQuery(
    string MarketingAddr,
    int M, 
    string ProfileAddr, 
    bool First,
    string? ParentAddr, 
    int? Pos,
    ulong Amount,
    string SenderAddr,
    ulong Fee) : IQuery<BuyPlaceByJettonBodyResponse>;
    

internal sealed class BuildBuyPlaceBodyQueryHandler(IMarketingQueries queries)
    : IQueryHandler<BuildBuyPlaceByJettonBodyQuery, BuyPlaceByJettonBodyResponse>
{
    public Task<Result<BuyPlaceByJettonBodyResponse>> Handle(BuildBuyPlaceByJettonBodyQuery request, CancellationToken ct)
        => Task.FromResult(queries.BuildBuyPlaceByJettonBody(
            queryId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            marketingAddr: request.MarketingAddr,
            m: request.M,
            profileAddr: request.ProfileAddr,
            first: request.First,
            parentAddr: request.ParentAddr,
            pos: request.Pos,
            amount: request.Amount,
            senderAddr: request.SenderAddr,
            fee: request.Fee));
}
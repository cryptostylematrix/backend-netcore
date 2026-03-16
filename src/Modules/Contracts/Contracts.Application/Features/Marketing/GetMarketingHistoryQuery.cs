namespace Contracts.Application.Features.Marketing;

public sealed record GetMarketingHistoryQuery(
    string Addr,
    uint Limit,
    ulong? Lt,
    string? Hash) : IQuery<MarketingTransactionHistoryResponse>;

internal sealed class GetMarketingHistoryHandler(IMarketingQueries marketingQueries)
    : IQueryHandler<GetMarketingHistoryQuery, MarketingTransactionHistoryResponse>
{
    public Task<Result<MarketingTransactionHistoryResponse>> Handle(GetMarketingHistoryQuery request, CancellationToken ct)
        => marketingQueries.GetMarketingHistoryAsync(
            addr: request.Addr,
            limit: request.Limit,
            lt: request.Lt,
            hash: request.Hash,
            ct: ct);
    
    
}
namespace Contracts.Application.Features.Marketing;

public sealed record GetMarketingDataQuery(string MarketingAddr) : IQuery<MarketingDataResponse>;

internal sealed class GetMultiDataQueryHandler(IMarketingQueries queries)
    : IQueryHandler<GetMarketingDataQuery, MarketingDataResponse>
{
    public Task<Result<MarketingDataResponse>> Handle(GetMarketingDataQuery request, CancellationToken ct)
        => queries.GetMarketingDataAsync(request.MarketingAddr, ct);
}
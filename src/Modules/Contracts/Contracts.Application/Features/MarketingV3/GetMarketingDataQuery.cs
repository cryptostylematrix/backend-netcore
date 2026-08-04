namespace Contracts.Application.Features.MarketingV3;

public sealed record GetMarketingDataQuery(string MarketingAddr) : IQuery<MarketingV3DataResponse>;

internal sealed class GetMarketingDataQueryHandler(IMarketingV3Queries queries)
    : IQueryHandler<GetMarketingDataQuery, MarketingV3DataResponse>
{
    public Task<Result<MarketingV3DataResponse>> Handle(GetMarketingDataQuery request, CancellationToken ct)
        => queries.GetMarketingDataAsync(request.MarketingAddr, ct);
}

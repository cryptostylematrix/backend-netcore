namespace Contracts.Application.Features.MarketingV3;

public sealed record GetBasicDataQuery(string MarketingAddr) : IQuery<MarketingV3BasicDataResponse>;

internal sealed class GetBasicDataQueryHandler(IMarketingV3Queries queries)
    : IQueryHandler<GetBasicDataQuery, MarketingV3BasicDataResponse>
{
    public Task<Result<MarketingV3BasicDataResponse>> Handle(GetBasicDataQuery request, CancellationToken ct)
        => queries.GetBasicDataAsync(request.MarketingAddr, ct);
}

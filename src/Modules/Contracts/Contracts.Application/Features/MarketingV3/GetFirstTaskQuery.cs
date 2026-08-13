namespace Contracts.Application.Features.MarketingV3;

public sealed record GetFirstTaskQuery(string MarketingAddr)
    : IQuery<MarketingV3FirstTaskResponse>;

internal sealed class GetFirstTaskQueryHandler(IMarketingV3Queries queries)
    : IQueryHandler<GetFirstTaskQuery, MarketingV3FirstTaskResponse>
{
    public Task<Result<MarketingV3FirstTaskResponse>> Handle(
        GetFirstTaskQuery request,
        CancellationToken ct) =>
        queries.GetFirstTaskAsync(request.MarketingAddr, ct);
}

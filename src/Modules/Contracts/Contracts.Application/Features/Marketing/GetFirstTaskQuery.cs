namespace Contracts.Application.Features.Marketing;


public sealed record GetFirstTaskQuery(string MarketingAddr) : IQuery<FirstTaskResponse>;

internal sealed class GetFirstTaskQueryHandler(IMarketingQueries queries)
    : IQueryHandler<GetFirstTaskQuery, FirstTaskResponse>
{
    public Task<Result<FirstTaskResponse>> Handle(GetFirstTaskQuery request, CancellationToken ct)
        => queries.GetFirstTaskAsync(request.MarketingAddr, ct);
}
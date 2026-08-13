namespace Contracts.Application.Features.MarketingV3;

public sealed record BuildProfileInfoQueryResponseQuery(
    ulong QueryId,
    uint TaskKey,
    MarketingV3ProfileInfo Profile) : IQuery<MarketingV3MessageBodyResponse>;

internal sealed class BuildProfileInfoQueryResponseQueryHandler(IMarketingV3Queries queries)
    : IQueryHandler<BuildProfileInfoQueryResponseQuery, MarketingV3MessageBodyResponse>
{
    public Task<Result<MarketingV3MessageBodyResponse>> Handle(
        BuildProfileInfoQueryResponseQuery request,
        CancellationToken ct) =>
        Task.FromResult(queries.SendProfileInfoQueryResponse(
            request.QueryId,
            request.TaskKey,
            request.Profile));
}

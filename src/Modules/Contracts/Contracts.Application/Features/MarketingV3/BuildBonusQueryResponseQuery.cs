namespace Contracts.Application.Features.MarketingV3;

public sealed record BuildBonusQueryResponseQuery(
    ulong QueryId,
    uint TaskKey,
    MarketingV3PlaceInfo Reason,
    MarketingV3ProfileData Recipient) : IQuery<MarketingV3MessageBodyResponse>;

internal sealed class BuildBonusQueryResponseQueryHandler(IMarketingV3Queries queries)
    : IQueryHandler<BuildBonusQueryResponseQuery, MarketingV3MessageBodyResponse>
{
    public Task<Result<MarketingV3MessageBodyResponse>> Handle(
        BuildBonusQueryResponseQuery request,
        CancellationToken ct) =>
        Task.FromResult(queries.SendBonusQueryResponse(
            request.QueryId,
            request.TaskKey,
            request.Reason,
            request.Recipient));
}

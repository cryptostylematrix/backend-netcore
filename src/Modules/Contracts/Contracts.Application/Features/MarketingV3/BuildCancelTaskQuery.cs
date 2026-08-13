namespace Contracts.Application.Features.MarketingV3;

public sealed record BuildCancelTaskQuery(
    ulong QueryId,
    uint TaskKey,
    string Comment) : IQuery<MarketingV3MessageBodyResponse>;

internal sealed class BuildCancelTaskQueryHandler(IMarketingV3Queries queries)
    : IQueryHandler<BuildCancelTaskQuery, MarketingV3MessageBodyResponse>
{
    public Task<Result<MarketingV3MessageBodyResponse>> Handle(
        BuildCancelTaskQuery request,
        CancellationToken ct) =>
        Task.FromResult(queries.SendCancelTask(
            request.QueryId,
            request.TaskKey,
            request.Comment));
}

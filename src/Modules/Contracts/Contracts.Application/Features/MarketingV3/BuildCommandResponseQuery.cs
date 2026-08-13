namespace Contracts.Application.Features.MarketingV3;

public sealed record BuildCommandResponseQuery(
    ulong QueryId,
    uint TaskKey,
    uint Code,
    MarketingV3SourcePlace Source) : IQuery<MarketingV3MessageBodyResponse>;

internal sealed class BuildCommandResponseQueryHandler(IMarketingV3Queries queries)
    : IQueryHandler<BuildCommandResponseQuery, MarketingV3MessageBodyResponse>
{
    public Task<Result<MarketingV3MessageBodyResponse>> Handle(
        BuildCommandResponseQuery request,
        CancellationToken ct) =>
        Task.FromResult(queries.SendCommandResponse(
            request.QueryId,
            request.TaskKey,
            request.Code,
            request.Source));
}

namespace Contracts.Application.Features.MarketingV3;

public sealed record BuildExecMessageBodyQuery(
    ulong QueryId,
    byte Structure,
    string ProfileAddr,
    uint CommandTag,
    string? PayloadBocHex) : IQuery<MarketingV3MessageBodyResponse>;

internal sealed class BuildExecMessageBodyQueryHandler(IMarketingV3Queries queries)
    : IQueryHandler<BuildExecMessageBodyQuery, MarketingV3MessageBodyResponse>
{
    public Task<Result<MarketingV3MessageBodyResponse>> Handle(
        BuildExecMessageBodyQuery request,
        CancellationToken ct) => Task.FromResult(queries.BuildExecMessageBody(
            request.QueryId,
            request.Structure,
            request.ProfileAddr.Trim(),
            request.CommandTag,
            request.PayloadBocHex?.Trim()));
}

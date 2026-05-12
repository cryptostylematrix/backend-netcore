namespace Contracts.Application.Features.Marketing;


public sealed record BuildCancelTaskBodyQuery(
    uint Key,
    string Comment) : IQuery<CancelTaskBodyResponse>;


internal sealed class BuildCancelTaskBodyQueryHandler(IMarketingQueries queries)
    : IQueryHandler<BuildCancelTaskBodyQuery, CancelTaskBodyResponse>
{
    public Task<Result<CancelTaskBodyResponse>> Handle(BuildCancelTaskBodyQuery request, CancellationToken ct)
        => Task.FromResult(queries.BuildCancelTaskBody(
            queryId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            key: request.Key,
            comment: request.Comment.Trim()));
}
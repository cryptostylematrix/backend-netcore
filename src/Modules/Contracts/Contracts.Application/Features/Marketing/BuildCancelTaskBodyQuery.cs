namespace Contracts.Application.Features.Marketing;


public sealed record BuildCancelTaskBodyQuery(
    ulong QueryId,
    uint Key,
    string Comment) : IQuery<CancelTaskBodyResponse>;


internal sealed class BuildCancelTaskBodyQueryHandler(IMarketingQueries queries)
    : IQueryHandler<BuildCancelTaskBodyQuery, CancelTaskBodyResponse>
{
    public Task<Result<CancelTaskBodyResponse>> Handle(BuildCancelTaskBodyQuery request, CancellationToken ct)
        => Task.FromResult(queries.BuildCancelTaskBody(
            queryId: request.QueryId,
            key: request.Key,
            comment: request.Comment.Trim()));
}
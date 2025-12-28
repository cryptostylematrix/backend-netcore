namespace Matrix.Application.Features.Locks;

public sealed record GetLocksQuery(short M, string ProfileAddr, int Page, int PageSize)
    : IQuery<Paginated<LockResponse>>;

internal sealed class GetLocksQueryHandler(ILockQueries lockQueries)
    : IQueryHandler<GetLocksQuery, Paginated<LockResponse>>
{
    public async Task<Result<Paginated<LockResponse>>> Handle(
        GetLocksQuery request,
        CancellationToken cancellationToken)
    {
        var page = await lockQueries.GetLocksAsync(
            request.M,
            request.ProfileAddr,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(page);
    }
}
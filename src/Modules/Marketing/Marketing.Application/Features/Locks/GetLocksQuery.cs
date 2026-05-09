namespace Marketing.Application.Features.Locks;

public sealed record GetLocksQuery(string MarketingAddr, byte M, string ProfileAddr, int Page, int PageSize)
    : IQuery<Paginated<LockResponse>>;

internal sealed class GetLocksQueryHandler(ILockQueries lockQueries)
    : IQueryHandler<GetLocksQuery, Paginated<LockResponse>>
{
    public async Task<Result<Paginated<LockResponse>>> Handle(
        GetLocksQuery request,
        CancellationToken cancellationToken)
    {
        var page = await lockQueries.GetLocksAsync(
            marketingAddr: request.MarketingAddr,
            m: request.M,
            profileAddr: request.ProfileAddr,
            page: request.Page,
            pageSize: request.PageSize,
            cancellationToken);

        return Result.Success(page);
    }
}
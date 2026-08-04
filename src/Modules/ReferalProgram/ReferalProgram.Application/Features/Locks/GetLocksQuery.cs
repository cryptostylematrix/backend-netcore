namespace ReferalProgram.Application.Features.Locks;

public sealed record GetLocksQuery(
    string MarketingAddr,
    byte StructNumber,
    string ProfileAddr,
    int Page,
    int PageSize) : IQuery<Paginated<LockResponse>>;

internal sealed class GetLocksQueryHandler(ILockQueries lockQueries)
    : IQueryHandler<GetLocksQuery, Paginated<LockResponse>>
{
    public async Task<Result<Paginated<LockResponse>>> Handle(
        GetLocksQuery request,
        CancellationToken cancellationToken)
    {
        var locks = await lockQueries.GetLocksAsync(
            request.MarketingAddr,
            request.StructNumber,
            request.ProfileAddr,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(locks);
    }
}

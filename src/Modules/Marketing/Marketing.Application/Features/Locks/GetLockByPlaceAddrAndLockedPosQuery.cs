namespace Marketing.Application.Features.Locks;

public sealed record GetLockByPlaceAddrAndLockedPosQuery(
    string MarketingAddr,
    string PlaceAddr,
    int LockedPos,
    string ProfileAddr)
    : IQuery<LockResponse>;

internal sealed class GetLockByPlaceAddrAndLockedPosQueryHandler(
    ILockQueries lockQueries)
    : IQueryHandler<GetLockByPlaceAddrAndLockedPosQuery, LockResponse>
{
    public async Task<Result<LockResponse>> Handle(
        GetLockByPlaceAddrAndLockedPosQuery request,
        CancellationToken cancellationToken)
    {
        var lockRow = await lockQueries.GetLockByPlaceAddrAndLockedPosAsync(
            marketingAddr: request.MarketingAddr,
            placeAddr: request.PlaceAddr,
            lockedPos: request.LockedPos,
            profileAddr: request.ProfileAddr,
            cancellationToken);

        return lockRow is null
            ? Result<LockResponse>.NotFound()
            : Result.Success(lockRow);
    }
}
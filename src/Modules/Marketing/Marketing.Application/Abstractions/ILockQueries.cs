namespace Marketing.Application.Abstractions;

public interface ILockQueries
{
    Task<string[]> GetAllLockMpsAsync(
        string marketingAddr,
        byte m,
        string profileAddr,
        CancellationToken ct);

    Task<Paginated<LockResponse>> GetLocksAsync(
        string marketingAddr,
        byte m,
        string profileAddr,
        int page,
        int pageSize,
        CancellationToken ct);
    
    Task<LockResponse?> GetLockByPlaceAddrAndLockedPosAsync(
        string marketingAddr,
        string placeAddr,
        uint lockedPos,
        string profileAddr,
        CancellationToken cancellationToken);
}
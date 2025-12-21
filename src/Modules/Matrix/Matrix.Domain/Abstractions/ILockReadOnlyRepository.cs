namespace Matrix.Domain.Abstractions;

public record LocksResult(IEnumerable<MultiLock> Items,  long Total);

public interface ILockReadOnlyRepository
{
    Task<LocksResult> GetLocks(short m, string profileAddr, int page, int pageSize);
    Task<MultiLock?> GetLockByPlaceAddrAndLockedPos(string placeAddr, short lockedPos, string profileAddr);
}
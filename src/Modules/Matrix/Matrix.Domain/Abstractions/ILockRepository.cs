namespace Matrix.Domain.Abstractions;

public interface ILockRepository: IRepository<MultiLock>
{
    Task<MultiLock> AddLock(MultiLock multiLock);
    Task<MultiLock> UpdateLockConfirm(int id);
    Task RemoveLock(int id);
}
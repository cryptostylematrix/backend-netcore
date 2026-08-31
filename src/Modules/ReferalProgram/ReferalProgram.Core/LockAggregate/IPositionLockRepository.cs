using Common.Domain;

namespace ReferalProgram.Core.LockAggregate;

public interface IPositionLockRepository : IRepository<PositionLock>
{
    Task<IReadOnlyList<PositionLock>> GetStructureLocksAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    Task<PositionLock?> GetAsync(
        string marketingAddr,
        byte structureNumber,
        string placeProfileAddr,
        uint placeNumber,
        string profileAddr,
        uint lockedPos,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PositionLock>> GetForPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        string placeProfileAddr,
        uint placeNumber,
        string profileAddr,
        CancellationToken cancellationToken);

    void Add(PositionLock positionLock);
    void Remove(PositionLock positionLock);

    void RemoveRange(IEnumerable<PositionLock> positionLocks) =>
        throw new NotSupportedException();
}

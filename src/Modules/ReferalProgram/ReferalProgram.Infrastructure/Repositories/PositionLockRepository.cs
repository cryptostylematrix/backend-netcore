using Microsoft.EntityFrameworkCore;
using ReferalProgram.Core.LockAggregate;
using ReferalProgram.Infrastructure.Persistence;

namespace ReferalProgram.Infrastructure.Repositories;

internal sealed class PositionLockRepository(DataContext dataContext)
    : IPositionLockRepository
{
    public async Task<IReadOnlyList<PositionLock>> GetStructureLocksAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken) =>
        await dataContext.PositionLocks
            .Where(positionLock => positionLock.MarketingAddr == marketingAddr
                && positionLock.StructureNumber == structureNumber)
            .OrderBy(positionLock => positionLock.Id)
            .ToListAsync(cancellationToken);

    public Task<PositionLock?> GetAsync(
        string marketingAddr,
        byte structureNumber,
        string placeProfileAddr,
        uint placeNumber,
        string profileAddr,
        uint lockedPos,
        CancellationToken cancellationToken) =>
        dataContext.PositionLocks.SingleOrDefaultAsync(
            positionLock => positionLock.MarketingAddr == marketingAddr
                && positionLock.StructureNumber == structureNumber
                && positionLock.PlaceProfileAddr == placeProfileAddr
                && positionLock.PlaceNumber == placeNumber
                && positionLock.ProfileAddr == profileAddr
                && positionLock.LockedPos == lockedPos,
            cancellationToken);

    public async Task<IReadOnlyList<PositionLock>> GetForPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        string placeProfileAddr,
        uint placeNumber,
        string profileAddr,
        CancellationToken cancellationToken) =>
        await dataContext.PositionLocks
            .AsNoTracking()
            .Where(positionLock => positionLock.MarketingAddr == marketingAddr
                && positionLock.StructureNumber == structureNumber
                && positionLock.PlaceProfileAddr == placeProfileAddr
                && positionLock.PlaceNumber == placeNumber
                && positionLock.ProfileAddr == profileAddr)
            .OrderBy(positionLock => positionLock.LockedPos)
            .ThenBy(positionLock => positionLock.Id)
            .ToListAsync(cancellationToken);

    public void Add(PositionLock positionLock) => dataContext.PositionLocks.Add(positionLock);

    public void Remove(PositionLock positionLock) => dataContext.PositionLocks.Remove(positionLock);

    public void RemoveRange(IEnumerable<PositionLock> positionLocks) =>
        dataContext.PositionLocks.RemoveRange(positionLocks);
}

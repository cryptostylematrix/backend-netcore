using Common.Domain;

namespace ReferalProgram.Core.LockAggregate;

public sealed class PositionLock : Entity, IAggregateRoot
{
    private PositionLock()
    {
    }

    private PositionLock(
        int taskKey,
        long taskQueryId,
        string? taskSourceAddr,
        string marketingAddr,
        byte structureNumber,
        string placeProfileAddr,
        uint placeNumber,
        string placeProfileLogin,
        string profileAddr,
        uint lockedPos,
        string mp,
        long createdAt)
    {
        TaskKey = taskKey;
        TaskQueryId = taskQueryId;
        TaskSourceAddr = taskSourceAddr;
        MarketingAddr = marketingAddr;
        StructureNumber = structureNumber;
        PlaceProfileAddr = placeProfileAddr;
        PlaceNumber = placeNumber;
        PlaceProfileLogin = placeProfileLogin;
        ProfileAddr = profileAddr;
        LockedPos = lockedPos;
        Mp = mp;
        CreatedAt = createdAt;
    }

    public int Id { get; private set; }
    public int TaskKey { get; private set; }
    public long TaskQueryId { get; private set; }
    public string? TaskSourceAddr { get; private set; }
    public string MarketingAddr { get; private set; } = null!;
    public byte StructureNumber { get; private set; }
    public string PlaceProfileAddr { get; private set; } = null!;
    public uint PlaceNumber { get; private set; }
    public string PlaceProfileLogin { get; private set; } = null!;
    public string ProfileAddr { get; private set; } = null!;
    public uint LockedPos { get; private set; }
    public string Mp { get; private set; } = null!;
    public long CreatedAt { get; private set; }

    public static PositionLock Create(
        int taskKey,
        long taskQueryId,
        string? taskSourceAddr,
        string marketingAddr,
        byte structureNumber,
        string placeProfileAddr,
        uint placeNumber,
        string placeProfileLogin,
        string profileAddr,
        uint lockedPos,
        string mp,
        long createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(marketingAddr);
        ArgumentException.ThrowIfNullOrWhiteSpace(placeProfileAddr);
        ArgumentException.ThrowIfNullOrWhiteSpace(placeProfileLogin);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileAddr);
        ArgumentException.ThrowIfNullOrWhiteSpace(mp);

        if (placeNumber == 0)
            throw new ArgumentOutOfRangeException(nameof(placeNumber));

        if (lockedPos == 0)
            throw new ArgumentOutOfRangeException(nameof(lockedPos));

        return new PositionLock(
            taskKey,
            taskQueryId,
            taskSourceAddr,
            marketingAddr,
            structureNumber,
            placeProfileAddr,
            placeNumber,
            placeProfileLogin,
            profileAddr,
            lockedPos,
            mp,
            createdAt);
    }

    public void RebuildMp(string mp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mp);
        Mp = mp;
    }
}

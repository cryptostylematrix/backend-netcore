namespace Matrix.Domain.Aggregates;

public sealed class MultiLock: Entity, IAggregateRoot
{
    public int Id { get; private set; }
    public string Mp { get; init; } = null!;
    public short M { get; init; }
    public string ProfileAddr { get; init; } = null!;
    public string PlaceAddr { get; init; } = null!;
    public short LockedPos { get; init; }
    public string PlaceProfileLogin { get; init; } = null!;
    public int PlaceNumber { get; init; }
    public long CreatedAt { get; init; }
    public bool Confirmed { get; init; } 
    
    public int TaskKey { get; init; }
    public long TaskQueryId { get; init; }
    public string TaskSourceAddr { get; init; } = null!;


    private MultiLock()
    {
    }

    private MultiLock(string mp, short m, string profileAddr, int taskKey, long taskQueryId, string taskSourceAddr,
        string placeAddr, short lockedPos, string placeProfileLogin, int placeNumber, long createdAt)
    {
        Mp = mp;
        M = m;
        ProfileAddr = profileAddr;
        TaskKey = taskKey;
        TaskQueryId = taskQueryId;
        TaskSourceAddr = taskSourceAddr;
        PlaceAddr = placeAddr;
        LockedPos = lockedPos;
        PlaceProfileLogin = placeProfileLogin;
        PlaceNumber = placeNumber;
        CreatedAt = createdAt;
    }
    
    public static Result<MultiLock> CreateMultiLock(string mp, short m, string profileAddr, int taskKey, long taskQueryId,
        string taskSourceAddr, string placeAddr, short lockedPos, string placeProfileLogin, int placeNumber, long createdAt)
    {
        // todo: validations

        var newLock = new MultiLock(
            mp: mp,
            m: m,
            profileAddr: profileAddr,
            taskKey: taskKey,
            taskQueryId: taskQueryId,
            taskSourceAddr: taskSourceAddr,
            placeAddr: placeAddr,
            lockedPos: lockedPos,
            placeProfileLogin: placeProfileLogin,
            placeNumber: placeNumber,
            createdAt: createdAt);
            
        return Result<MultiLock>.Success(newLock);
    }
}
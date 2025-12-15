namespace Matrix.Domain.Aggregates;

public sealed class MultiLock: Entity, IAggregateRoot
{
    public int Id { get; private set; }
    public string Mp { get; init; } = null!;
    public int M { get; init; }
    public string ProfileAddr { get; init; } = null!;
    public string PlaceAddr { get; init; } = null!;
    public int LockedPos { get; init; }
    public string PlaceProfileLogin { get; init; } = null!;
    public int PlaceNumber { get; init; }
    public int CreatedAt { get; init; }
    public bool Confirmed { get; init; } = false;
    
    public int TaskKey { get; init; }
    public int TaskQueryId { get; init; }
    public string TaskSourceAddr { get; init; } = null!;


    private MultiLock()
    {
    }

    private MultiLock(string mp, int m, string profileAddr, int taskKey, int taskQueryId, string taskSourceAddr,
        string placeAddr, int lockedAos, string placeProfileLogin, int placeNumber, int createdAt)
    {
        Mp = mp;
        M = m;
        ProfileAddr = profileAddr;
        TaskKey = taskKey;
        TaskQueryId = taskQueryId;
        TaskSourceAddr = taskSourceAddr;
        PlaceAddr = placeAddr;
        LockedPos = lockedAos;
        PlaceProfileLogin = placeProfileLogin;
        PlaceNumber = placeNumber;
        CreatedAt = createdAt;
    }
    
    public static Result<MultiLock> CreateMultiLock(string mp, int m, string profileAddr, int taskKey, int taskQueryId, string taskSourceAddr,
        string placeAddr, int lockedAos, string placeProfileLogin, int placeNumber, int createdAt)
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
            lockedAos: lockedAos,
            placeProfileLogin: placeProfileLogin,
            placeNumber: placeNumber,
            createdAt: createdAt);
            
        return Result<MultiLock>.Success(newLock);
    }
}
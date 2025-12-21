namespace Matrix.Domain.Aggregates;

public sealed class MultiPlace: Entity, IAggregateRoot
{
    public int Id { get; init; }
    public int? ParentId { get; init; }

    public int TaskKey { get; init; }          // integer
    public long TaskQueryId { get; init; }     // bigint
    public string? TaskSourceAddr { get; init; }

    public short M { get; init; }              // smallint (or int if you prefer)
    public string Mp { get; init; } = null!;
    public short Pos { get; init; }            // smallint (0/1)
    public string Addr { get; init; } = null!;
    public string ProfileAddr { get; init; } = null!;
    public string? InviterProfileAddr { get; init; }
    public string? ParentAddr { get; init; }

    public int PlaceNumber { get; init; }      // integer
    public long CreatedAt { get; init; }       // bigint (craeted_at)
    public short Filling { get; init; }        // smallint
    public short Filling2 { get; init; }       // smallint
    public short Clone { get; init; }          // smallint (0/1)

    public string ProfileLogin { get; init; } = null!;
    public string Index { get; init; } = null!;
    public bool Confirmed { get; init; }

    private MultiPlace()
    {
    }

    private MultiPlace(short m, string profileAddr, string addr, string? parentAddr, int? parentId,
        string mp, short pos, int placeNumber, long createdAt, short clone, string profileLogin,
        int taskKey, long taskQueryId, string? taskSourceAddr, string inviterProfileAddr)
    {
        M = m;
        ProfileAddr = profileAddr;
        Addr = addr;
        ParentAddr = parentAddr;
        ParentId = parentId;
        Mp = mp;
        Pos = pos;
        PlaceNumber = placeNumber;
        CreatedAt = createdAt;
        Clone = clone;
        ProfileLogin = profileLogin;
        TaskKey = taskKey;
        TaskQueryId = taskQueryId;
        TaskSourceAddr = taskSourceAddr;
        InviterProfileAddr = inviterProfileAddr;
    }
    
    
    public static Result<MultiPlace> CreateMultiPlace(
        short m, string profileAddr, string addr, string? parentAddr, int? parentId,
        string mp, short pos, int placeNumber, long createdAt, short clone, string profileLogin,
        int taskKey, long taskQueryId, string? taskSourceAddr, string inviterProfileAddr)
    {
        // todo: validations


        var newPlace = new MultiPlace(
            m: m,
            profileAddr: profileAddr,
            addr: addr,
            parentAddr: parentAddr,
            parentId: parentId,
            mp: mp,
            pos: pos,
            placeNumber: placeNumber,
            createdAt: createdAt,
            clone: clone,
            profileLogin: profileLogin,
            taskKey: taskKey,
            taskQueryId: taskQueryId,
            taskSourceAddr: taskSourceAddr,
            inviterProfileAddr: inviterProfileAddr);
            
        return Result<MultiPlace>.Success(newPlace);
    }
    
    
}
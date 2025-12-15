namespace Matrix.Domain.Aggregates;

public sealed class MultiPlace: Entity, IAggregateRoot
{
    public int Id { get; init; }
    public int? ParentId { get; init; }
    public int TaskKey { get; init; }
    public int TaskQueryId { get; init; }
    public string? TaskSourceAddr { get; init; }
    
    public int M { get; init; }
    public string Mp { get; init; } = null!;
    public int Pos { get; init; }
    public string Addr { get; init; } = null!;
    public string ProfileAddr { get; init; } = null!;
    public string? InviterProfileAddr { get; init; }
    public string? ParentAddr { get; init; }
    public int PlaceNumber { get; init; }
    public int CreatedAt { get; init; }
    public int Filling { get; init; }
    public int Filling2 { get; init; }
    public int Clone { get; init; }
    public string ProfileLogin { get; init; } = null!;
    public string Index { get; init; } = null!;
    public bool Confirmed { get; init; } = false;

    private MultiPlace()
    {
    }

    private MultiPlace(int m, string profileAddr, string addr, string? parentAddr, int? parentId,
        string mp, int pos, int placeNumber, int createdAt, int clone, string profileLogin,
        int taskKey, int taskQueryId, string? taskSourceAddr, string inviterProfileAddr)
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
        int m, string profileAddr, string addr, string? parentAddr, int? parentId,
        string mp, int pos, int placeNumber, int createdAt, int clone, string profileLogin,
        int taskKey, int taskQueryId, string? taskSourceAddr, string inviterProfileAddr)
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
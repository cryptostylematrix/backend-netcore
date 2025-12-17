namespace Matrix.Application.Features.Matrix;

public abstract class TreeNodeResponse
{
    public abstract string Kind { get; }
    public bool Locked { get; init; }
    public bool CanLock { get; init; }
    public bool IsLock { get; init; }
    public TreeNodeResponse[]? Children { get; init; }
    public string? ParentAddr { get; init; }
    public uint Pos { get; init; }
}

public sealed class TreeEmptyNodeResponse : TreeNodeResponse
{
    public override string Kind => "empty";

    public bool IsNextPos { get; init; }
    public bool CanBuy { get; init; }
}

public sealed class TreeFilledNodeResponse : TreeNodeResponse
{
    public override string Kind => "filled";

    public string Addr { get; init; } = null!;
    public uint PlaceNumber { get; init; }
    public uint Clone { get; init; }
    public ulong CreatedAt  { get; init; }
    public string Login { get; init; } = null!;
    public string ImageUrl { get; init; } = null!;
    
    public uint Descendants { get; init; }
    public bool IsRoot { get; init; }
}
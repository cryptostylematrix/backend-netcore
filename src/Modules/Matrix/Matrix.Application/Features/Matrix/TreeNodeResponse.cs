namespace Matrix.Application.Features.Matrix;

public abstract class TreeNodeResponse
{
    [JsonPropertyName("kind")]
    public abstract string Kind { get; }
    
    [JsonPropertyName("locked")]
    public bool Locked { get; init; }
    
    [JsonPropertyName("can_lock")]
    public bool CanLock { get; init; }
    
    [JsonPropertyName("is_lock")]
    public bool IsLock { get; init; }
    
    [JsonPropertyName("parent_addr")]
    public string? ParentAddr { get; init; }
    
    [JsonPropertyName("pos")]
    public short Pos { get; init; }
    
    [JsonPropertyName("children")]
    public TreeNodeResponse[]? Children { get; init; }
}

public sealed class TreeEmptyNodeResponse : TreeNodeResponse
{
    public override string Kind => "empty";

    [JsonPropertyName("is_next_pos")]
    public bool IsNextPos { get; init; }
    
    [JsonPropertyName("can_buy")]
    public bool CanBuy { get; init; }
}

public sealed class TreeFilledNodeResponse : TreeNodeResponse
{
    public override string Kind => "filled";

    [JsonPropertyName("addr")]
    public string Addr { get; init; } = null!;
    
    [JsonPropertyName("place_number")]
    public int PlaceNumber { get; init; }
    
    [JsonPropertyName("clone")]
    public short Clone { get; init; }
    
    [JsonPropertyName("created_at")]
    public long CreatedAt  { get; init; }
    
    [JsonPropertyName("login")]
    public string Login { get; init; } = null!;
    
    [JsonPropertyName("image_url")]
    public string ImageUrl { get; init; } = null!;
    
    [JsonPropertyName("descendants")]
    public uint Descendants { get; init; }
    
    [JsonPropertyName("is_root")]
    public bool IsRoot { get; init; }
}
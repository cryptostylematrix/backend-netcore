namespace Matrix.Dto;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(TreeEmptyNodeResponse), "empty")]
[JsonDerivedType(typeof(TreeFilledNodeResponse), "filled")]
public abstract class TreeNodeResponse
{
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
    [JsonPropertyName("is_next_pos")]
    public bool IsNextPos { get; init; }
    
    [JsonPropertyName("can_buy")]
    public bool CanBuy { get; init; }
}

public sealed class TreeFilledNodeResponse : TreeNodeResponse
{
    [JsonPropertyName("addr")]
    public string Addr { get; init; } = null!;
    
    [JsonPropertyName("place_number")]
    public int PlaceNumber { get; init; }
    
    [JsonPropertyName("clone")]
    public short Clone { get; init; }
    
    [JsonPropertyName("created_at")]
    public long CreatedAt  { get; init; }
    
    [JsonPropertyName("profile_login")]
    public string ProfileLogin { get; init; } = null!;
    
    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    
    [JsonPropertyName("descendants")]
    public long Descendants { get; init; }
    
    [JsonPropertyName("is_root")]
    public bool IsRoot { get; init; }
}
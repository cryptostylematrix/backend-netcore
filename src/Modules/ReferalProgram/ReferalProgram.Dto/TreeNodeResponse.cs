namespace ReferalProgram.Dto;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "node_type")]
[JsonDerivedType(typeof(TreeEmptyNodeResponse), "empty")]
[JsonDerivedType(typeof(TreeFilledNodeResponse), "filled")]
public abstract class TreeNodeResponse
{
    [JsonPropertyName("locked")]
    public bool Locked { get; init; }

    [JsonPropertyName("is_lock")]
    public bool IsLock { get; init; }

    [JsonPropertyName("can_lock")]
    public bool CanLock { get; init; }

    [JsonPropertyName("can_unlock")]
    public bool CanUnlock { get; init; }

    [JsonPropertyName("parent_profile_addr")]
    public string? ParentProfileAddr { get; init; }

    [JsonPropertyName("parent_place_number")]
    public uint? ParentPlaceNumber { get; init; }

    [JsonPropertyName("pos")]
    public uint Pos { get; init; }

    [JsonPropertyName("width")]
    public byte Width { get; init; }

    [JsonPropertyName("height")]
    public byte Height { get; init; }

    [JsonPropertyName("children")]
    public TreeNodeResponse[]? Children { get; init; }
}

public sealed class TreeEmptyNodeResponse : TreeNodeResponse
{
    [JsonPropertyName("is_next_pos")]
    public bool IsNextPos { get; init; }

    [JsonPropertyName("can_buy")]
    public bool CanBuy { get; init; }

    [JsonPropertyName("buy_command_tag")]
    public uint? BuyCommandTag { get; init; }

    [JsonPropertyName("include_position")]
    public bool IncludePosition { get; init; }
}

public sealed class TreeFilledNodeResponse : TreeNodeResponse
{
    [JsonPropertyName("place_number")]
    public uint PlaceNumber { get; init; }

    [JsonPropertyName("profile_addr")]
    public string? ProfileAddr { get; init; }

    [JsonPropertyName("profile_login")]
    public string? ProfileLogin { get; init; }

    [JsonPropertyName("kind")]
    public byte Kind { get; init; }

    [JsonPropertyName("filling")]
    public uint Filling { get; init; }

    [JsonPropertyName("matrix_places_count")]
    public long MatrixPlacesCount { get; init; }

    [JsonPropertyName("descendants")]
    public long Descendants { get; init; }

    [JsonPropertyName("level")]
    public uint Level { get; init; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; init; }

    [JsonPropertyName("activated_at")]
    public long? ActivatedAt { get; init; }

    [JsonPropertyName("is_root")]
    public bool IsRoot { get; init; }
}

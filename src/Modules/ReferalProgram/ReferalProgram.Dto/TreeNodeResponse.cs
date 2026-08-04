namespace ReferalProgram.Dto;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "node_type")]
[JsonDerivedType(typeof(TreeEmptyNodeResponse), "empty")]
[JsonDerivedType(typeof(TreeFilledNodeResponse), "filled")]
public abstract class TreeNodeResponse
{
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

public sealed class TreeEmptyNodeResponse : TreeNodeResponse;

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

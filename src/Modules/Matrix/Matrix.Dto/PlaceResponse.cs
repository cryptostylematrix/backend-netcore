namespace Matrix.Dto;

public sealed class PlaceResponse
{
    // ---------- internal-only (never serialized) ----------

    [JsonIgnore]
    public int Id { get; init; }

    [JsonIgnore]
    public int? ParentId { get; init; }

    [JsonIgnore]
    public string Mp { get; init; } = null!;

    // used by TreeInfo.CanLock and NextPos logic
    [JsonIgnore]
    public short Filling { get; init; }

    // ---------- API fields ----------

    [JsonPropertyName("addr")]
    public string Addr { get; init; } = null!;

    [JsonPropertyName("parent_addr")]
    public string ParentAddr { get; init; } = null!;

    [JsonPropertyName("place_number")]
    public int PlaceNumber { get; init; }

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; init; }

    // RENAMED: was FillCount → now Filling2
    [JsonPropertyName("fill_count")]
    public short Filling2 { get; init; }

    [JsonPropertyName("clone")]
    public short Clone { get; init; }

    [JsonPropertyName("pos")]
    public short Pos { get; init; }

    [JsonPropertyName("login")]
    public string ProfileLogin { get; init; } = null!;

    [JsonPropertyName("m")]
    public short M { get; init; }

    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
}
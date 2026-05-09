namespace Marketing.Dto;

public sealed class PlaceResponse
{
    // ---------- internal-only (never serialized) ----------

    [JsonIgnore]
    public int Id { get; init; }

    [JsonIgnore]
    public int? ParentId { get; init; }

    [JsonIgnore]
    public string Mp { get; init; } = null!;

    // ---------- API fields ----------

    [JsonPropertyName("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    
    [JsonPropertyName("m")]
    public byte M { get; init; }
    
    [JsonPropertyName("parent_addr")]
    public string? ParentAddr { get; init; }
    
    [JsonPropertyName("pos")]
    public uint Pos { get; init; }
    
    [JsonPropertyName("seq_no")]
    public uint SeqNo { get; init; }
    
    [JsonPropertyName("width")]
    public byte Width { get; init; }
    
    [JsonPropertyName("height")]
    public byte Height { get; init; }
    
    [JsonPropertyName("kind")]
    public byte Kind { get; init; }
    
    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    
    [JsonPropertyName("place_number")]
    public uint PlaceNumber { get; init; }
    
    [JsonPropertyName("addr")]
    public string Addr { get; init; } = null!;
    
    //------ additional
    [JsonPropertyName("created_at")]
    public long CreatedAt { get; init; }

    [JsonPropertyName("login")]
    public string ProfileLogin { get; init; } = null!;
}
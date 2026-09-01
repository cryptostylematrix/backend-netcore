namespace ReferalProgram.Dto;

public class PlaceResponse
{
    // ---------- internal-only (never serialized) ----------

    [JsonIgnore]
    public int Id { get; init; }

    [JsonIgnore]
    public int? ParentId { get; init; }

    [JsonIgnore]
    public string Mp { get; init; } = null!;

    [JsonIgnore]
    public byte PosGroup {get;init;}

    // ---------- API fields ----------

    [JsonPropertyName("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    
    [JsonPropertyName("struct")]
    public byte StructNumber { get; init; }

    [JsonPropertyName("profile_addr")]
    public string? ProfileAddr { get; init; } = null!;
    
    [JsonPropertyName("place_number")]
    public uint PlaceNumber { get; init; }

    [JsonPropertyName("profile_login")]
    public string? ProfileLogin { get; init; } = null!;

    [JsonPropertyName("index")]
    public string Index { get; init; } = null!;


    [JsonPropertyName("parent_profile_addr")]
    public string? ParentProfileAddr { get; init; } = null!;

    [JsonPropertyName("parent_profile_login")]
    public string? ParentProfileLogin { get; init; }
    
    [JsonPropertyName("parent_place_number")]
    public uint? ParentPlaceNumber { get; init; }


    [JsonPropertyName("created_at")]
    public long CreatedAt { get; init; }

    [JsonPropertyName("activated_at")]
    public long? ActivatedAt { get; init; }

    [JsonPropertyName("ative")]
    public bool IsActive {get; init;}

    [JsonPropertyName("kind")]
    public byte Kind { get; init; }

    [JsonPropertyName("pos")]
    public uint Pos { get; init; }
    
    [JsonPropertyName("filling")]
    public uint Filling { get; init; }
    
    [JsonPropertyName("deep")]
    public uint Deep { get; init; }

}
    

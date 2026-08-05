namespace ReferalProgram.Dto;

public sealed class NextPosResponse
{
    // ---------- internal-only ----------
    [JsonIgnore]
    public string Mp { get; init; } = null!;

    [JsonIgnore]
    public byte PosGroup { get; init; }

    // ---------- API fields ----------
    [JsonPropertyName("profile_addr")]
    public string? ProfileAddr { get; init; }

    [JsonPropertyName("place_number")]
    public uint PlaceNumber { get; init; }

    [JsonPropertyName("pos")]
    public uint Pos { get; init; }
}

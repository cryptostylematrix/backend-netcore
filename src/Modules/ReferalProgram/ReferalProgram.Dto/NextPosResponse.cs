namespace ReferalProgram.Dto;

public sealed class NextPosResponse
{
    // ---------- internal-only ----------
    [JsonIgnore]
    public string Mp { get; init; } = null!;

    // ---------- API fields ----------
    [JsonPropertyName("profile_addr")]
    public string? ProfileAddr { get; init; }

    [JsonPropertyName("place_number")]
    public uint PlaceNumber { get; init; }

    [JsonPropertyName("pos")]
    public uint Pos { get; init; }
}

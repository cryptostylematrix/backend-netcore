namespace Marketing.Dto;

public sealed class NextPosResponse
{
    // ---------- internal-only ----------
    [JsonIgnore]
    public string Mp { get; init; } = null!;

    // ---------- API fields ----------
    [JsonPropertyName("parent_addr")]
    public string ParentAddr { get; init; } = null!;

    [JsonPropertyName("pos")]
    public uint Pos { get; init; }
}
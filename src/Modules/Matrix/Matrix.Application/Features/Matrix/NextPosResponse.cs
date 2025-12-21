namespace Matrix.Application.Features.Matrix;

public sealed class NextPosResponse
{
    [JsonPropertyName("parent_addr")]
    public string ParentAddr { get; init; } = null!;
    
    [JsonPropertyName("pos")]
    public short Pos { get; init; }
}
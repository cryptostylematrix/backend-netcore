namespace Matrix.Application.Features.Matrix;

public sealed class NextPosResponse
{
    public string ParentAddr { get; init; } = null!;
    public int Pos { get; init; }
}
namespace Matrix.Presentation.Endpoints.Matrix.GetNextPos;

public sealed class GetNextPosRequest
{
    public int M { get; init; }
    public string ProfileAddr { get; init; } = null!;
}
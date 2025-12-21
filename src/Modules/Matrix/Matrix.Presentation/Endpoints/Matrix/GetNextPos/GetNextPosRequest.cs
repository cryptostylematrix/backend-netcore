namespace Matrix.Presentation.Endpoints.Matrix.GetNextPos;

public sealed class GetNextPosRequest
{
    public short M { get; init; }
    public string ProfileAddr { get; init; } = null!;
}
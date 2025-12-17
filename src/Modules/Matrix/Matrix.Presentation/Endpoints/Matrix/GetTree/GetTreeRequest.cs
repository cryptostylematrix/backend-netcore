namespace Matrix.Presentation.Endpoints.Matrix.GetTree;

public sealed class GetTreeRequest
{
    public string ProfileAddr { get; init; } = null!;
    public string PlaceAddr { get; init; } = null!;
}
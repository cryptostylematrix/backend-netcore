namespace Matrix.Presentation.Endpoints.Places.GetRootPlace;

public sealed class GetRootPlaceRequest
{
    public int M { get; init; }
    public string ProfileAddr { get; init; } = null!;
}
namespace Matrix.Presentation.Endpoints.Places.GetPlaces;

public sealed class GetPlacesRequest
{
    public int M { get; init; }
    public string ProfileAddr { get; init; } = null!;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
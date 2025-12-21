namespace Matrix.Presentation.Endpoints.Places.SearchPaces;

public sealed class SearchPacesRequest
{
    public short M { get; init; }
    public string ProfileAddr { get; init; } = null!;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string Query { get; init; } = null!;
}
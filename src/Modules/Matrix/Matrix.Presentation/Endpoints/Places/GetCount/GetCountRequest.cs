namespace Matrix.Presentation.Endpoints.Places.GetCount;

public sealed class GetCountRequest
{
    public int M { get; init; }
    public string ProfileAddr { get; init; } = null!;
}
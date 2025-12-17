namespace Matrix.Application.Features.Places;

public sealed class PlaceResponse
{
    public string Addr { get; init; } = null!;
    public string ParentAddr { get; init; } = null!;
    public int PlaceNumber { get; init; }
    public int CreatedAt { get; init; }
    public int FillCount { get;  init; }
    public int Clone { get; init; }
    public int Pos { get; init; }
    public string Login { get; init; } = null!;
    public int M { get; init; }
    public string ProfileAddr { get; init; } = null!;
}
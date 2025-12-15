namespace Matrix.Application.Features.Places;

public sealed class PlaceResponse
{
    public string Addr { get; private set; } = null!;
    public string ParentAddr { get; private set; } = null!;
    public int PlaceNumber { get; private set; }
    public int CreatedAt { get; private set; }
    public int FillCount { get;  private set; }
    public int Clone { get; private set; }
    public int Pos { get; private set; }
    public string Login { get; private set; } = null!;
    public int M { get; private set; }
    public string ProfileAddr { get; private set; } = null!;
}
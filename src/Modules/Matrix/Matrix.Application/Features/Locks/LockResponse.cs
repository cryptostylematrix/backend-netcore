namespace Matrix.Application.Features.Locks;

public sealed class LockResponse
{
    public int M { get; init; }
    public string ProfileAddr { get; init; } = null!;
    public string PlaceAddr { get; init; } = null!;
    public int LockedPos { get; init; }
    public string PlaceProfileLogin { get; init; } = null!;
    public int PlaceNumber { get; init; }
    public int CreatedAt { get; init; }
}
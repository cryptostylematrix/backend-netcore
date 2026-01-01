namespace Contracts.Presentation.Endpoints.Multi.BuildLockPosBody;

public sealed class BuildLockPosBodyRequest
{
    public int M { get; init; }
    public string ProfileAddr { get; init; } = null!;
    public string ParentAddr { get; init; } = null!;
    public int Pos { get; init; }
}
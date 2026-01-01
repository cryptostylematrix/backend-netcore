namespace Contracts.Presentation.Endpoints.Multi.BuildUnlockPosBody;

public sealed class BuildUnlockPosBodyRequest
{
    public int M { get; init; }
    public string ProfileAddr { get; init; } = null!;
    public string ParentAddr { get; init; } = null!;
    public int Pos { get; init; }
}
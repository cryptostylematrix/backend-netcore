namespace Contracts.Presentation.Endpoints.Marketing.BuildUnlockPosBody;

public sealed class BuildUnlockPosBodyRequest
{
    [BindFrom("m")]
    public int M { get; init; }
    
    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    
    [BindFrom("parent_addr")]
    public string ParentAddr { get; init; } = null!;
    
    [BindFrom("pos")]
    public int Pos { get; init; }
}
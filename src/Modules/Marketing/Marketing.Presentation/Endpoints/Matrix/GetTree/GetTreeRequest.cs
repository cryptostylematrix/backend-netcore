namespace Marketing.Presentation.Endpoints.Matrix.GetTree;

public sealed class GetTreeRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    
    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    
    [BindFrom("place_addr")]
    public string PlaceAddr { get; init; } = null!;
    
    [BindFrom("from_pos")]
    public uint FromPos { get; init; }
    
    [BindFrom("to_pos")]
    public uint ToPos { get; init; }
}
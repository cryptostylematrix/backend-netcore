namespace Marketing.Presentation.Endpoints.Places.GetRootPlace;

public sealed class GetRootPlaceRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    
    [BindFrom("m")]
    public byte M { get; init; }
    
    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
}
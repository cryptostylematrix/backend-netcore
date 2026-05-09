namespace Marketing.Presentation.Endpoints.Places.GetTotalCount;

public sealed class GetTotalCountRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    
    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
}
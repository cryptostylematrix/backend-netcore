namespace Marketing.Presentation.Endpoints.Places.GetPath;

public sealed class GetPathRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    
    [BindFrom("root_addr")]
    public string RootAddr { get; init; } = null!; 
    
    [BindFrom("place_addr")]
    public string PlaceAddr { get; init; } = null!; 
}
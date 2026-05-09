namespace Marketing.Presentation.Endpoints.Places.SearchPaces;

public sealed class SearchPacesRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    
    [BindFrom("m")]
    public byte M { get; init; }
    
    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    
    [BindFrom("page")]
    public int  Page { get; init; } = 1;
    
    [BindFrom("page_size")]
    public int PageSize { get; init; } = 20;
    
    [BindFrom("query")]
    public string Query { get; init; } = null!;
}
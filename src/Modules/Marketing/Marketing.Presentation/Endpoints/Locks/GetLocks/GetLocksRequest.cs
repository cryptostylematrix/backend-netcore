namespace Marketing.Presentation.Endpoints.Locks.GetLocks;

public class GetLocksRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    
    [BindFrom("m")]
    public byte M { get; init; }
    
    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    
    [BindFrom("page")]
    public int Page { get; init; } = 1;
    
    [BindFrom("page_size")]
    public int PageSize { get; init; } = 20;
}
namespace Contracts.Presentation.Endpoints.Marketing.BuildBuyPlaceByJettonBody;

public class BuildBuyPlaceByJettonBodyRequest
{
    public string MarketingAddr { get; init; } = null!;
    
    public int M { get; init; }
    public string ProfileAddr { get; init; } = null!;
    public bool First { get; init; }
    public string? ParentAddr { get; init; }
    public int? Pos { get; init; }
    
    public ulong Amount { get; init; }
    public string SenderAddr { get; init; } = null!;
    public ulong Fee  { get; init; }
}
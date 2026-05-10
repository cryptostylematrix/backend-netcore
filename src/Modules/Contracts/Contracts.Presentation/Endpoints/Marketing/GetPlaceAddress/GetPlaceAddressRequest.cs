using System.Text.Json.Serialization;

namespace Contracts.Presentation.Endpoints.Marketing.GetPlaceAddress;

public sealed class GetPlaceAddressRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    
    [BindFrom("m")]
    public int M { get; init; }
    
    [BindFrom("parent_addr")]
    public string ParentAddr { get; init; } = null!;
    
    [BindFrom("pos")]
    public int Pos { get; init; }
}
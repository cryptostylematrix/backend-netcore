namespace Contracts.Presentation.Endpoints.Marketing.BuildBuyPlaceByTonBody;

public class BuildBuyPlaceByTonBodyRequest
{
    public int M { get; init; }
    public string ProfileAddr { get; init; } = null!;
    public bool First { get; init; }
    public string? ParentAddr { get; init; }
    public int? Pos { get; init; }
}
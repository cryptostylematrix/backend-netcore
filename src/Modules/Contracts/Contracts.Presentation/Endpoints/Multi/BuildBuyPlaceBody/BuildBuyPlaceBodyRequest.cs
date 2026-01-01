namespace Contracts.Presentation.Endpoints.Multi.BuildBuyPlaceBody;

public sealed class BuildBuyPlaceBodyRequest
{
    public int M { get; init; }
    public string ProfileAddr { get; init; } = null!;
    public string? ParentAddr { get; init; }
    public int? Pos { get; init; }
}
namespace ReferalProgram.Presentation.Endpoints.Places.SearchPlaces;

public sealed class SearchPlacesRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("structure_number")]
    public byte StructureNumber { get; init; }

    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;

    [BindFrom("query")]
    public string Query { get; init; } = null!;

    [BindFrom("page")]
    public int Page { get; init; } = 1;

    [BindFrom("page_size")]
    public int PageSize { get; init; } = 20;
}

namespace ReferalProgram.Application.Features.Places;

public sealed record SearchPlacesQuery(
    string MarketingAddr,
    byte StructureNumber,
    string ViewerProfileAddr,
    string Query,
    int Page,
    int PageSize) : IQuery<Paginated<PlaceResponse>>;

internal sealed class SearchPlacesQueryHandler(
    IPlaceQueries placeQueries,
    IStructureQueries structureQueries,
    IPositionAlgorithmConfigurationParser configurationParser,
    IPositionRootResolver positionRootResolver)
    : IQueryHandler<SearchPlacesQuery, Paginated<PlaceResponse>>
{
    public async Task<Result<Paginated<PlaceResponse>>> Handle(
        SearchPlacesQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ViewerProfileAddr))
            return Result<Paginated<PlaceResponse>>.Error("ViewerProfileAddr is required.");

        var structure = await structureQueries.GetStructureAsync(
            request.MarketingAddr,
            request.StructureNumber,
            cancellationToken);
        if (structure is null)
            return Result<Paginated<PlaceResponse>>.NotFound();

        var configuration = configurationParser.Parse(structure.PosAlgo);
        var root = await positionRootResolver.ResolveAsync(
            configuration.Root,
            request.MarketingAddr,
            request.StructureNumber,
            request.ViewerProfileAddr,
            cancellationToken);
        if (root is null)
            return Result<Paginated<PlaceResponse>>.NotFound();

        var places = await placeQueries.SearchPlacesAsync(
            request.MarketingAddr,
            request.StructureNumber,
            root.Mp,
            request.Query.Trim(),
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(places);
    }
}

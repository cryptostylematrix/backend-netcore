namespace ReferalProgram.Application.Features.Places;

public sealed record GetPathQuery(
    string MarketingAddr,
    byte StructureNumber,
    string ViewerProfileAddr,
    string? TargetProfileAddr,
    uint TargetPlaceNumber) : IQuery<IReadOnlyCollection<PlaceResponse>>;

internal sealed class GetPathQueryHandler(
    IPlaceQueries placeQueries,
    IStructureQueries structureQueries,
    IPositionAlgorithmConfigurationParser configurationParser,
    IPositionRootResolver positionRootResolver)
    : IQueryHandler<GetPathQuery, IReadOnlyCollection<PlaceResponse>>
{
    public async Task<Result<IReadOnlyCollection<PlaceResponse>>> Handle(
        GetPathQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ViewerProfileAddr))
            return Result<IReadOnlyCollection<PlaceResponse>>.Error("ViewerProfileAddr is required.");

        var structure = await structureQueries.GetStructureAsync(
            request.MarketingAddr,
            request.StructureNumber,
            cancellationToken);
        if (structure is null)
            return Result<IReadOnlyCollection<PlaceResponse>>.NotFound();

        var configuration = configurationParser.Parse(structure.PosAlgo);
        var root = await positionRootResolver.ResolveAsync(
            configuration.Root,
            request.MarketingAddr,
            request.StructureNumber,
            request.ViewerProfileAddr,
            cancellationToken);
        if (root is null)
            return Result<IReadOnlyCollection<PlaceResponse>>.NotFound();

        var path = await placeQueries.GetPathAsync(
            request.MarketingAddr,
            request.StructureNumber,
            root.ProfileAddr,
            root.PlaceNumber,
            string.IsNullOrWhiteSpace(request.TargetProfileAddr) ? null : request.TargetProfileAddr,
            request.TargetPlaceNumber,
            cancellationToken);

        return path is null
            ? Result<IReadOnlyCollection<PlaceResponse>>.NotFound()
            : Result.Success<IReadOnlyCollection<PlaceResponse>>(path);
    }
}

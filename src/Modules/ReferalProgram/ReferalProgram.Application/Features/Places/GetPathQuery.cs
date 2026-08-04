namespace ReferalProgram.Application.Features.Places;

public sealed record GetPathQuery(
    string MarketingAddr,
    byte StructureNumber,
    string? FromProfileAddr,
    uint FromPlaceNumber,
    string? ToProfileAddr,
    uint ToPlaceNumber) : IQuery<IReadOnlyCollection<PlaceResponse>>;

internal sealed class GetPathQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetPathQuery, IReadOnlyCollection<PlaceResponse>>
{
    public async Task<Result<IReadOnlyCollection<PlaceResponse>>> Handle(
        GetPathQuery request,
        CancellationToken cancellationToken)
    {
        var path = await placeQueries.GetPathAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.FromProfileAddr,
            request.FromPlaceNumber,
            request.ToProfileAddr,
            request.ToPlaceNumber,
            cancellationToken);

        return path is null
            ? Result<IReadOnlyCollection<PlaceResponse>>.NotFound()
            : Result.Success<IReadOnlyCollection<PlaceResponse>>(path);
    }
}

namespace Matrix.Application.Features.Places;

public sealed record GetPathQuery(string RootAddr, string PlaceAddr)
    : IQuery<IEnumerable<PlaceResponse>>;

internal sealed class GetPathQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetPathQuery, IEnumerable<PlaceResponse>>
{
    public async Task<Result<IEnumerable<PlaceResponse>>> Handle(GetPathQuery request, CancellationToken ct)
    {
        // If you kept the simple nullable return:
        var path = await placeQueries.GetPathAsync(request.RootAddr, request.PlaceAddr, ct);

        if (path is null)
            return Result<IEnumerable<PlaceResponse>>.NotFound();

        // If you want to return Invalid on m mismatch, switch to PathResult as noted above.
        return Result<IEnumerable<PlaceResponse>>.Success(path);
    }
}
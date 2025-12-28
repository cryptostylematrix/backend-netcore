namespace Matrix.Application.Features.Places;

public sealed record GetCountQuery(short M, string ProfileAddr) : IQuery<PlacesCountResponse>;

internal sealed class GetCountQueryHandler(IPlaceQueries placeQueries)
    : IQueryHandler<GetCountQuery, PlacesCountResponse>
{
    public async Task<Result<PlacesCountResponse>> Handle(GetCountQuery request, CancellationToken cancellationToken)
    {
        var count = await placeQueries.GetPlacesCountAsync(
            request.M,
            request.ProfileAddr,
            cancellationToken);

        return Result.Success(new PlacesCountResponse { Count = count });
    }
}
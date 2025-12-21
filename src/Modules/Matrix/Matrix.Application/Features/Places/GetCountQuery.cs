namespace Matrix.Application.Features.Places;

public sealed record GetCountQuery(short M, string ProfileAddr): IQuery<PlacesCountResponse>;

internal sealed class GetCountQueryHandler(IPlaceReadOnlyRepository placeReadOnlyRepository) : 
    IQueryHandler<GetCountQuery, PlacesCountResponse>
{
    public async Task<Result<PlacesCountResponse>> Handle(GetCountQuery request, CancellationToken cancellationToken)
    {
        var count = await placeReadOnlyRepository.GetPlacesCount(request.M, request.ProfileAddr);
        
        return Result.Success(new PlacesCountResponse
        {
            Count = count
        });
    }
}

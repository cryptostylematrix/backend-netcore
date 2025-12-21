namespace Matrix.Application.Features.Places;

public record GetRootPlaceQuery(short M, string ProfileAddr): IQuery<PlaceResponse>;

internal sealed class GetRootPlaceQueryHandler(IPlaceReadOnlyRepository placeReadOnlyRepo, IMapper mapper) : 
    IQueryHandler<GetRootPlaceQuery, PlaceResponse>
{
    public async Task<Result<PlaceResponse>> Handle(GetRootPlaceQuery request, CancellationToken cancellationToken)
    {
        var rootPlace = await placeReadOnlyRepo.GetRootPlace(request.M, request.ProfileAddr);
        if (rootPlace is null)
        {
            return Result<PlaceResponse>.NotFound();
        }
        
        var response = mapper.Map<PlaceResponse>(rootPlace);
        return Result.Success(response);
    }
}
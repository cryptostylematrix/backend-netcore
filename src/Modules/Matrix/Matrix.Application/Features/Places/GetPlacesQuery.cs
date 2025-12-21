namespace Matrix.Application.Features.Places;

public sealed record GetPlacesQuery(short M, string ProfileAddr, int Page, int PageSize): 
    IQuery<Paginated<PlaceResponse>>;
    
    
internal sealed class GetPlacesQueryHandler(IPlaceReadOnlyRepository placeReadOnlyRepository, IMapper mapper) : 
    IQueryHandler<GetPlacesQuery, Paginated<PlaceResponse>>
{
    public async Task<Result<Paginated<PlaceResponse>>> Handle(GetPlacesQuery request, CancellationToken cancellationToken)
    {
        var placesResult = await placeReadOnlyRepository.GetPlaces(
            m: request.M, 
            profileAddr: request.ProfileAddr, 
            page: request.Page, 
            pageSize: request.PageSize);

        var response = new Paginated<PlaceResponse>
        {
            Page = request.Page,
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)placesResult.Total / request.PageSize)),
            Items = mapper.Map<IEnumerable<PlaceResponse>>(placesResult.Items)
        };

        return Result.Success(response);
    }
}


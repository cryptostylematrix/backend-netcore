namespace Matrix.Application.Features.Places;


public sealed record SearchPacesQuery(short M, string ProfileAddr, int Page, int PageSize, string Query): 
    IQuery<Paginated<PlaceResponse>>;
    
    
internal sealed class SearchPacesQueryHandler(IPlaceReadOnlyRepository placeReadOnlyRepository, IMapper mapper) : 
    IQueryHandler<SearchPacesQuery, Paginated<PlaceResponse>>
{
    public async Task<Result<Paginated<PlaceResponse>>> Handle(SearchPacesQuery request, CancellationToken cancellationToken)
    {
        var placesResult = await placeReadOnlyRepository.SearchPlaces(
            m: request.M,
            profileAddr: request.ProfileAddr,
            page: request.Page,
            pageSize: request.PageSize,
            query: request.Query);

        var response = new Paginated<PlaceResponse>
        {
            Page = request.Page,
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)placesResult.Total / request.PageSize)),
            Items = mapper.Map<IEnumerable<PlaceResponse>>(placesResult.Items)
        };

        return Result.Success(response);
    }
}
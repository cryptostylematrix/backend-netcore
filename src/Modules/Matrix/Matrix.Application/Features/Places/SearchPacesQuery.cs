namespace Matrix.Application.Features.Places;


public sealed record SearchPacesQuery(int M, string ProfileAddr, int Page, int PageSize, string Query): 
    IQuery<Paginated<PlaceResponse>>;
    
    
internal sealed class SearchPacesQueryHandler : IQueryHandler<SearchPacesQuery, Paginated<PlaceResponse>>
{
    public Task<Result<Paginated<PlaceResponse>>> Handle(SearchPacesQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
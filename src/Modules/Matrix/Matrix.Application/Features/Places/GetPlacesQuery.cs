namespace Matrix.Application.Features.Places;

public sealed record GetPlacesQuery(int M, string ProfileAddr, int Page, int PageSize): 
    IQuery<Paginated<PlaceResponse>>;
    
    
internal sealed class GetPlacesQueryHandler : IQueryHandler<GetPlacesQuery, Paginated<PlaceResponse>>
{
    public Task<Result<Paginated<PlaceResponse>>> Handle(GetPlacesQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
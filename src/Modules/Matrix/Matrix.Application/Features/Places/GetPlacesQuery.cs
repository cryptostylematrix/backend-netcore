namespace Matrix.Application.Features.Places;

public sealed record GetPlacesQuery(int M, string ProfileAddr): 
    IRequest<Paginated<PlaceResponse>>;
    
    
internal sealed class GetPlacesQueryHandler : IRequestHandler<GetPlacesQuery, Paginated<PlaceResponse>>
{
    public Task<Paginated<PlaceResponse>> Handle(GetPlacesQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
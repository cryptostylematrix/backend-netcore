namespace Matrix.Application.Features.Places;

public sealed record GetPathQuery(string RootAddr, string PlaceAddr): 
    IRequest<IEnumerable<PlaceResponse>>;
    
    
internal sealed class GetPathQueryHandler : IRequestHandler<GetPathQuery, IEnumerable<PlaceResponse>>
{
    public Task<IEnumerable<PlaceResponse>> Handle(GetPathQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
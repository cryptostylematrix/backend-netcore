namespace Matrix.Application.Features.Places;

public sealed record GetPathQuery(string RootAddr, string PlaceAddr): 
    IQuery<IEnumerable<PlaceResponse>>;
    
    
internal sealed class GetPathQueryHandler : IQueryHandler<GetPathQuery, IEnumerable<PlaceResponse>>
{
    public Task<Result<IEnumerable<PlaceResponse>>> Handle(GetPathQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
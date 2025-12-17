namespace Matrix.Application.Features.Matrix;

public sealed record GetTreeQuery(string ProfileAddr, string PlaceAddr) : IQuery<TreeNodeResponse>;
    
    
internal sealed class GetPlacesQueryHandler : IQueryHandler<GetTreeQuery, TreeNodeResponse>
{
    public Task<Result<TreeNodeResponse>> Handle(GetTreeQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
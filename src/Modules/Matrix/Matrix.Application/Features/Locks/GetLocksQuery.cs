namespace Matrix.Application.Features.Locks;


public sealed record GetLocksQuery(int M, string ProfileAddr, int Page, int PageSize): 
    IQuery<Paginated<LockResponse>>;
    
    
internal sealed class GetLocksQueryHandler : IQueryHandler<GetLocksQuery, Paginated<LockResponse>>
{
    public Task<Result<Paginated<LockResponse>>> Handle(GetLocksQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
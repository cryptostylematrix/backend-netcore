namespace Matrix.Application.Features.Locks;


public sealed record GetLocksQuery(short M, string ProfileAddr, int Page, int PageSize): 
    IQuery<Paginated<LockResponse>>;
    
    
internal sealed class GetLocksQueryHandler(ILockReadOnlyRepository lockRepository, IMapper mapper) 
    : IQueryHandler<GetLocksQuery, Paginated<LockResponse>>
{
    public async Task<Result<Paginated<LockResponse>>> Handle(GetLocksQuery request, CancellationToken cancellationToken)
    {
        var locksResult = await lockRepository.GetLocks(
            m: request.M, 
            profileAddr: request.ProfileAddr, 
            page: request.Page, 
            pageSize: request.PageSize);

        var response = new Paginated<LockResponse>
        {
            Page = request.Page,
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)locksResult.Total / request.PageSize)),
            Items = mapper.Map<IEnumerable<LockResponse>>(locksResult.Items)
        };

        return Result.Success(response);
    }
}
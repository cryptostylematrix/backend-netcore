namespace Contracts.Application.Features.Multi;

    
public sealed record BuildLockPosBodyQuery(
    int M, 
    string ProfileAddr, 
    string ParentAddr, 
    int Pos) : IQuery<LockPosBodyResponse>;
    
       
internal sealed class BuildLockPosBodyQueryHandler(IMultiQueries queries)
    : IQueryHandler<BuildLockPosBodyQuery, LockPosBodyResponse>
{
    public Task<Result<LockPosBodyResponse>> Handle(BuildLockPosBodyQuery request, CancellationToken ct)
        => Task.FromResult(queries.BuildLockPosBody(
            queryId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            m: request.M,
            profileAddr: request.ProfileAddr,
            parentAddr: request.ParentAddr,
            pos: request.Pos));
}

namespace Contracts.Application.Features.Marketing;

public sealed record BuildUnlockPosBodyQuery(
    int M, 
    string ProfileAddr, 
    string ParentAddr, 
    int Pos) : IQuery<UnlockPosBodyResponse>;
    
       
internal sealed class BuildUnlockPosBodyQueryHandler(IMarketingQueries queries)
    : IQueryHandler<BuildUnlockPosBodyQuery, UnlockPosBodyResponse>
{
    public Task<Result<UnlockPosBodyResponse>> Handle(BuildUnlockPosBodyQuery request, CancellationToken ct)
        => Task.FromResult(queries.BuildUnlockPosBody(
            queryId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            m: request.M,
            profileAddr: request.ProfileAddr,
            parentAddr: request.ParentAddr,
            pos: request.Pos));
}
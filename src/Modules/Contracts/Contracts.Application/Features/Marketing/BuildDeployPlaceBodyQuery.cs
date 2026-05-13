namespace Contracts.Application.Features.Marketing;

public sealed record BuildDeployPlaceBodyQuery(
    ulong QueryId,
    uint Key,
    string ParentAddr,
    byte Kind ,
    string ProfileAddr,
    uint PlaceNumber,
    string? InviterProfileAddr) : IQuery<DeployPlaceBodyResponse>;
    

internal sealed class BuildDeployPlaceBodyQueryHandler(IMarketingQueries queries)
    : IQueryHandler<BuildDeployPlaceBodyQuery, DeployPlaceBodyResponse>
{
    public Task<Result<DeployPlaceBodyResponse>> Handle(BuildDeployPlaceBodyQuery request, CancellationToken ct)
        => Task.FromResult(queries.BuildDeployPlaceBody(
            queryId: request.QueryId,
            key: request.Key,
            parentAddr: request.ParentAddr,
            kind: request.Kind,
            profileAddr: request.ProfileAddr,
            placeNumber: request.PlaceNumber,
            inviterProfileAddr: request.InviterProfileAddr));
}
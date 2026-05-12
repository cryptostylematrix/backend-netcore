namespace Contracts.Application.Features.Marketing;

public sealed record BuildDeployPlaceBodyQuery(
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
            queryId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            key: request.Key,
            parentAddr: request.ParentAddr,
            kind: request.Kind,
            profileAddr: request.ProfileAddr,
            placeNumber: request.PlaceNumber,
            inviterProfileAddr: request.InviterProfileAddr));
}
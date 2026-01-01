namespace Contracts.Application.Features.ProfileCollection;

public sealed record BuildDeployItemBodyQuery( string Login,
    string? ImageUrl,
    string? FirstName,
    string? LastName,
    string? TgUsername) : IQuery<DeployItemBodyResponse>;


internal sealed class BuildDeployItemBodyQueryHandler(IProfileCollectionQueries queries)
    : IQueryHandler<BuildDeployItemBodyQuery, DeployItemBodyResponse>
{
    public Task<Result<DeployItemBodyResponse>> Handle(BuildDeployItemBodyQuery request, CancellationToken ct)
        => Task.FromResult(queries.BuildDeployItemBody(
            queryId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            login: request.Login,
            imageUrl: request.ImageUrl,
            firstName: request.FirstName,
            lastName: request.LastName,
            tgUsername: request.TgUsername));
}
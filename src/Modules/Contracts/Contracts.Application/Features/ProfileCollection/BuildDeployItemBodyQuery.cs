using Contracts.Application.Validation;

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
    {
        ct.ThrowIfCancellationRequested();
        var login = request.Login?.Trim().ToLowerInvariant() ?? "";
        if (!ProfileLogin.IsValid(login))
            return Task.FromResult(Result<DeployItemBodyResponse>.Invalid(new ValidationError
            {
                Identifier = nameof(request.Login),
                ErrorMessage = "Use 4–20 English letters, digits or hyphens, starting and ending with a letter or digit."
            }));

        return Task.FromResult(queries.BuildDeployItemBody(
            queryId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            login: login,
            imageUrl: request.ImageUrl,
            firstName: request.FirstName,
            lastName: request.LastName,
            tgUsername: request.TgUsername));
    }
}
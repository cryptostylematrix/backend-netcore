namespace Contracts.Presentation.Endpoints.ProfileCollection.BuildDeployItemBody;

public sealed class BuildDeployItemBodyRequest
{
    public string Login { get; init; } = null!;
    public string? ImageUrl { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? TgUsername { get; init; }
}
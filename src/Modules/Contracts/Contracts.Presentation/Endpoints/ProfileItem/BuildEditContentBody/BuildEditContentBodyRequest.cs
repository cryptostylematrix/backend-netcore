namespace Contracts.Presentation.Endpoints.ProfileItem.BuildEditContentBody;

public sealed class BuildEditContentBodyRequest
{
    public string Login { get; init; } = null!;
    public string? ImageUrl { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? TgUsername { get; init; }
}
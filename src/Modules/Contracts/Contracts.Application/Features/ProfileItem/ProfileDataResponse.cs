namespace Contracts.Application.Features.ProfileItem;

public sealed class ProfileDataResponse
{
    public int IsInit { get; init; }
    public string Index { get; init; } = null!;
    public string CollectionAddr { get; init; } = null!;
    public string? OwnerAddr { get; init; }
    public ProfileContentResponse? Content { get; init; }
}

public sealed class ProfileContentResponse
{
    public string Login { get; init; } = null!;
    public string? ImageUrl { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? TgUsername { get; init; }
}
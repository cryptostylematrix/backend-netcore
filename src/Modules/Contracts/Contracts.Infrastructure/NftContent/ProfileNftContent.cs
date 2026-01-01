namespace Contracts.Infrastructure.NftContent;

public static class ProfileNftContent
{
    public static NftContentOnchain ProfileToNftContent(
        string login,
        string? imageUrl = null,
        string? firstName = null,
        string? lastName = null,
        string? tgUsername = null
    )
    {
        var formattedLogin = login.ToLowerForProfile()
                             ?? throw new ArgumentException("login is required", nameof(login));

        var formattedImageUrl = imageUrl.NormalizeImage();
        var formattedFirstName = firstName.Capitalize() ?? string.Empty;
        var formattedLastName = lastName.Capitalize() ?? string.Empty;
        var formattedTgUsername = tgUsername.ToLowerForProfile() ?? string.Empty;

        var attributes = new[]
        {
            new { trait_type = "firstName",  value = formattedFirstName },
            new { trait_type = "lastName",   value = formattedLastName },
            new { trait_type = "tgUsername", value = formattedTgUsername },
        };

        return new NftContentOnchain(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["name"] = formattedLogin,
            ["description"] = "Crypto Style Profile",
            ["image"] = formattedImageUrl,
            ["attributes"] = JsonSerializer.Serialize(attributes)
        });
    }
}
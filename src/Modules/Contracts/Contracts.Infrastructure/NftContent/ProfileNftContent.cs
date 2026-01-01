namespace Contracts.Infrastructure.NftContent;

public static class ProfileNftContent
{
    private const string DefaultImage = "https://cryptostylematrix.github.io/frontend/cs-big.png";

    public static NftContentOnchain ProfileToNftContent(
        string login,
        string? imageUrl = null,
        string? firstName = null,
        string? lastName = null,
        string? tgUsername = null
    )
    {
        var formattedLogin = ToLower(login)
                             ?? throw new ArgumentException("login is required", nameof(login));

        var formattedImageUrl = NormalizeImage(imageUrl);
        var formattedFirstName = Capitalize(firstName) ?? string.Empty;
        var formattedLastName = Capitalize(lastName) ?? string.Empty;
        var formattedTgUsername = ToLower(tgUsername) ?? string.Empty;

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



    private static string NormalizeImage(string? value)
    {
        var lower = value?.Trim().ToLowerInvariant();
        return !string.IsNullOrEmpty(lower) ? lower : DefaultImage;
    }

    private static string? Capitalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var t = value.Trim();
        return t.Length switch
        {
            0 => null,
            1 => t.ToUpperInvariant(),
            _ => char.ToUpperInvariant(t[0]) + t[1..].ToLowerInvariant()
        };
    }

    private static string? ToLower(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var t = value.Trim();
        return t.Length > 0 ? t.ToLowerInvariant() : null;
    }
}
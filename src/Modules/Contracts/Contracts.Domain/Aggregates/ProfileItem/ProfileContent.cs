using System.Security.Cryptography;
using System.Text;

namespace Contracts.Domain.Aggregates.ProfileItem;

public sealed class ProfileContent
{
    public string Login { get; init; } = null!;
    public string? ImageUrl { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? TgUsername { get; init; }
    
    
    private const string DefaultImage = "https://cryptostylematrix.github.io/frontend/cs-big.png";

    public static ProfileContent? FromCell(Cell? content)
    {
        if (content is null) return null;

        var fields = new Dictionary<string, string?>(StringComparer.Ordinal);

        try
        {
            var slice = content.Parse();
            var start = (uint)slice.LoadUInt(8);
            if (start != 0)
            {
                throw new Exception("Unknown on-chain content format");
            }

            var dict = slice.LoadDict(NftDictValue.DictLoadingOptions);


            var keys = new[] { "image", "name", "description", "attributes" };
            foreach (var key in keys)
            {
                var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
                var dictValue = dict.Get(new Bits(keyHash));
                if (dictValue is not null)
                {
                    fields[key] = Encoding.UTF8.GetString(dictValue.Content);
                }
            }
        }
        catch
        {
            // Replace with your logger if needed
            return null;
        }

        var attrs = ProfileContentAttrs.ExtractAttributes(fields.GetValueOrDefault("attributes"));

        var normalizedImageUrl =
            StringHelper.ToLower(fields.GetValueOrDefault("image")) is { Length: > 0 } li
                ? li
                : DefaultImage;

        var normalizedFirstName = StringHelper.Capitalize(attrs.FirstName);
        var normalizedLastName = StringHelper.Capitalize(attrs.LastName);
        var normalizedTgUsername = StringHelper.ToLower(attrs.TgUsername);

        var normalizedLogin =
            StringHelper.SanitizeLogin(attrs.Login ?? (fields.GetValueOrDefault("name")))
            ?? "unknown";

        return new ProfileContent
        {
            Login = normalizedLogin,
            ImageUrl = normalizedImageUrl,
            FirstName = normalizedFirstName,
            LastName = normalizedLastName,
            TgUsername = normalizedTgUsername,
        };
    }
}
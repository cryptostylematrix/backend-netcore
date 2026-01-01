namespace Contracts.Infrastructure.NftContent;

public static class StringHelpers
{
    private const string DefaultImage = "https://cryptostylematrix.github.io/frontend/cs-big.png";
    
    extension(string? value)
    {
        public string NormalizeImage()
        {
            var lower = value?.Trim().ToLowerInvariant();
            return !string.IsNullOrEmpty(lower) ? lower : DefaultImage;
        }

        public string? Capitalize()
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

        public string? ToLowerForProfile()
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var t = value.Trim();
            return t.Length > 0 ? t.ToLowerInvariant() : null;
        }
    }
}
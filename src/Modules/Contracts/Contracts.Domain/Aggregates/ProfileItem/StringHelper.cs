using System.Text;

namespace Contracts.Domain.Aggregates.ProfileItem;

public static class StringHelper
{
    public static string? ToLower(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var t = value.Trim();
        return t.Length > 0 ? t.ToLowerInvariant() : null;
    }

    public static string? SanitizeLogin(string? value)
    {
        if (value == null) return null;

        // Drop non-printable characters, trim, and lower
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            int code = ch;
            if (code is >= 32 and <= 126)
                sb.Append(ch);
        }

        var cleaned = sb.ToString().Trim().ToLowerInvariant();
        return cleaned.Length > 0 ? cleaned : null;
    }

    public static string? Capitalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length switch
        {
            0 => null,
            1 => trimmed.ToUpperInvariant(),
            _ => char.ToUpperInvariant(trimmed[0]) + trimmed[1..].ToLowerInvariant()
        };
    }
}
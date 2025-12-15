using System.Text.Json;

namespace Contracts.Domain.Aggregates.ProfileItem;

public sealed class ProfileContentAttrs
{
    public string? FirstName { get; private init; }
    public string? LastName { get; private init; }
    public string? TgUsername { get; private init; }
    public string? Login { get; private init; }

    public static ProfileContentAttrs ExtractAttributes(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new ProfileContentAttrs();

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return new ProfileContentAttrs();

            return new ProfileContentAttrs
            {
                FirstName = GetValue("firstName"),
                LastName = GetValue("lastName"),
                TgUsername = GetValue("tgUsername"),
                Login = GetValue("login")
            };

            string? GetValue(string trait)
            {
                foreach (var el in doc.RootElement.EnumerateArray().Where(el => el.ValueKind == JsonValueKind.Object))
                {
                    if (!el.TryGetProperty("trait_type", out var tt)) continue;
                    if (tt.ValueKind != JsonValueKind.String) continue;
                    if (!string.Equals(tt.GetString(), trait, StringComparison.Ordinal)) continue;

                    if (el.TryGetProperty("value", out var v))
                    {
                        return v.ValueKind switch
                        {
                            JsonValueKind.String => v.GetString(),
                            _ => v.ToString()
                        };
                    }
                }

                return null;
            }
        }
        catch
        {
            return new ProfileContentAttrs();
        }
    }
}


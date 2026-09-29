using System.Globalization;
using System.Text;
using System.Text.Json;

namespace UI.Application.Features.Avatars;

// Embedded outlines keep indexer rendering independent of installed system fonts.
internal static class AvatarLettering
{
    private static readonly Dictionary<string, Dictionary<string, Glyph>> Fonts = Load();

    internal sealed record Glyph(string Path, double Advance);
    internal sealed record Outline(string Paths, double Width, double FontSize);

    internal static Outline Create(string text, string font, double maxFontSize, double maxWidth, double spacing = 0)
    {
        var glyphs = Fonts[font];
        var advance = text.Sum(character => glyphs[character.ToString()].Advance) + spacing * (text.Length - 1);
        var size = Math.Min(maxFontSize, maxWidth / advance);
        var paths = new StringBuilder();
        var x = 0d;
        foreach (var character in text)
        {
            var glyph = glyphs[character.ToString()];
            if (glyph.Path.Length > 0)
                paths.Append(CultureInfo.InvariantCulture,
                    $"<path transform=\"translate({x:0.#####} 0)\" d=\"{glyph.Path}\"/>");
            x += glyph.Advance + spacing;
        }
        return new Outline(paths.ToString(), advance * size, size);
    }

    internal static string Place(Outline outline, double baseline) =>
        FormattableString.Invariant($"translate({256 - outline.Width / 2:0.#####} {baseline:0.#####}) scale({outline.FontSize:0.#####})");

    private static Dictionary<string, Dictionary<string, Glyph>> Load()
    {
        using var stream = typeof(AvatarLettering).Assembly.GetManifestResourceStream("UI.AvatarLettering.json")
            ?? throw new InvalidOperationException("Avatar lettering resource is missing.");
        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Glyph>>>(stream)!;
    }
}

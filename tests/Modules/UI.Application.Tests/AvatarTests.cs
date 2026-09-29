using System.Xml.Linq;
using UI.Application.Features.Avatars;
using Xunit;

namespace UI.Application.Tests;

public sealed class AvatarTests
{
    [Fact]
    public async Task AvatarUsesKnownNftHashAndCanonicalLogin()
    {
        var handler = new GetAvatarQueryHandler();
        var result = await handler.Handle(new GetAvatarQuery("alice"), default);
        var canonical = await handler.Handle(new GetAvatarQuery(" ALICE "), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(result.Value, canonical.Value);
        Assert.Contains("hsl(64, 90%, 82%)", result.Value);
        Assert.Equal("svg", XDocument.Parse(result.Value).Root!.Name.LocalName);
        var other = await handler.Handle(new GetAvatarQuery("bobby"), default);
        Assert.NotEqual(result.Value, other.Value);
    }

    [Fact]
    public async Task MaximumLengthLoginIsRenderedInFull()
    {
        const string login = "abcdefghijklmnopqrst";
        Assert.Equal(20, login.Length);
        var result = await new GetAvatarQueryHandler().Handle(new GetAvatarQuery(login), default);
        Assert.True(result.IsSuccess);
        var svg = XDocument.Parse(result.Value);
        var label = Assert.Single(svg.Descendants(XName.Get("g", "http://www.w3.org/2000/svg")).Where(element => (string?)element.Attribute("id") == "login"));
        Assert.Equal(login, (string?)label.Attribute("aria-label"));
        Assert.Equal(login.Length, label.Elements(XName.Get("path", "http://www.w3.org/2000/svg")).Count());
        var outline = AvatarLettering.Create(login, "Login", 64, 416);
        Assert.InRange(outline.Width, 1, 416.001);
        Assert.DoesNotContain("<text", result.Value);
        Assert.DoesNotContain("<filter", result.Value);
    }

    [Theory]
    [InlineData("abcdefghijklmnopqrstuvwxyz", "Login")]
    [InlineData("0123456789-", "Login")]
    [InlineData("CRYPTO STYLE", "Brand")]
    public void EmbeddedOutlinesCoverEverySupportedCharacter(string text, string font)
    {
        var outline = AvatarLettering.Create(text, font, 64, 416);
        Assert.InRange(outline.Width, 1, 416.001);
        var paths = XElement.Parse($"<g>{outline.Paths}</g>").Elements("path").ToArray();
        Assert.Equal(text.Count(character => character != ' '), paths.Length);
        Assert.All(paths, path => Assert.False(string.IsNullOrWhiteSpace((string?)path.Attribute("d"))));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-alice")]
    [InlineData("alice-")]
    [InlineData("abcdefghijklmnopqrstu")]
    [InlineData("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwx")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("al ice")]
    [InlineData("ali_ce")]
    [InlineData("алиса")]
    [InlineData("alicé")]
    [InlineData("alice🐸")]
    public async Task InvalidLoginsCannotProduceSvg(string login)
    {
        var result = await new GetAvatarQueryHandler().Handle(new GetAvatarQuery(login), default);
        Assert.False(result.IsSuccess);
    }
}

using System.Text.Json;
using ReferalProgram.Application.Abstractions;

namespace ReferalProgram.Application.Tests;

public sealed class ActivitySettingsTests
{
    [Theory]
    [InlineData(0, "invite")]
    [InlineData(1, "marketing")]
    [InlineData(255, "marketing")]
    public void Missing_options_have_legacy_defaults(byte number, string type)
    {
        var settings = Parse($"{{\"type\":\"{type}\"}}", number);
        Assert.Equal(type, settings.Type);
        Assert.False(settings.PreserveStatusOnActivation);
    }

    [Theory]
    [InlineData("{}", false)]
    [InlineData("{\"set_active_on_activation\":true}", false)]
    [InlineData("{\"set_active_on_activation\":false}", true)]
    [InlineData("{\"activation_sync\":\"group\"}", false)]
    [InlineData("{\"legacy_unknown\":123}", false)]
    public void Legacy_configuration_retains_its_meaning(string json, bool preserve)
    {
        foreach (byte number in new byte[] { 0, 1, 4, 255 })
        {
            var settings = Parse(json, number);
            Assert.Equal(preserve, settings.PreserveStatusOnActivation);
        }
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("true")]
    [InlineData("{\"type\":null}")]
    [InlineData("{\"type\":1}")]
    [InlineData("{\"type\":\"matrix\"}")]
    [InlineData("{\"type\":\"invite\"}")]
    [InlineData("{\"type\":\"marketing\",\"unknown\":false}")]
    [InlineData("{\"type\":\"marketing\",\"set_active_on_activation\":true}")]
    [InlineData("{\"type\":\"marketing\",\"preserve_status_on_activation\":null}")]
    [InlineData("{\"type\":\"marketing\",\"preserve_status_on_activation\":\"false\"}")]
    [InlineData("{\"type\":\"marketing\",\"when_inactive\":null}")]
    [InlineData("{\"type\":\"marketing\",\"spillover\":[]}")]
    [InlineData("{\"type\":\"marketing\",\"when_inactive\":{\"allow_inviting_with_places\":true}}")]
    [InlineData("{\"type\":\"marketing\",\"spillover\":{\"require_active_invite\":1}}")]
    [InlineData("{\"type\":\"marketing\",\"spillover\":{\"unknown\":false}}")]
    [InlineData("{\"type\":\"marketing\",\"type\":\"marketing\"}")]
    [InlineData("{\"type\":\"marketing\",\"spillover\":{\"require_active_invite\":false,\"require_active_invite\":true}}")]
    [InlineData("{\"preserve_status_on_activation\":true}")]
    [InlineData("{\"when_inactive\":{}}")]
    [InlineData("{\"spillover\":{}}")]
    [InlineData("{\"set_active_on_activation\":null}")]
    public void Rejects_invalid_configuration(string json) =>
        Assert.Throws<JsonException>(() => Parse(json, 1));

    [Theory]
    [InlineData("{\"type\":\"marketing\"}")]
    [InlineData("{\"type\":\"invite\",\"spillover\":{}}")]
    [InlineData("{\"type\":\"invite\",\"when_inactive\":{\"allow_own_children\":false}}")]
    public void Invite_rejects_marketing_settings(string json) =>
        Assert.Throws<JsonException>(() => Parse(json, 0));

    [Theory]
    [InlineData("allow_as_bonus_recipient")]
    [InlineData("allow_as_clone_recipient")]
    [InlineData("keep_on_compression")]
    public void Recognizes_each_invite_rule(string property) =>
        AssertRule(property, ActivitySettings.ParseRecipientRules(JsonSerializer.Deserialize<JsonElement>($"{{\"type\":\"invite\",\"when_inactive\":{{\"{property}\":true}}}}"), 0)!);

    [Theory]
    [InlineData("when_inactive", "allow_as_bonus_recipient")]
    [InlineData("when_inactive", "allow_as_clone_recipient")]
    [InlineData("when_inactive", "keep_on_compression")]
    public void Recognizes_each_marketing_rule(string block, string property) =>
        AssertRule(property, ActivitySettings.ParseRecipientRules(JsonSerializer.Deserialize<JsonElement>($"{{\"type\":\"marketing\",\"{block}\":{{\"{property}\":true}}}}"), 1)!);

    private static void AssertRule(string property, InactiveRecipientSettings rules)
    {
        Assert.Equal(property == "allow_as_bonus_recipient", rules.AllowAsBonusRecipient);
        Assert.Equal(property == "allow_as_clone_recipient", rules.AllowAsCloneRecipient);
        Assert.Equal(property == "keep_on_compression", rules.KeepOnCompression);
    }

    private static ActivitySettings Parse(string json, byte number) =>
        ActivitySettings.Parse(JsonSerializer.Deserialize<JsonElement>(json), number);
}

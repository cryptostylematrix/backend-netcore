namespace UI.Dto;

public sealed class WalletProfileResponse
{
    [JsonPropertyName("wallet_addr")]
    public string WalletAddr { get; init; } = null!;

    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;

    [JsonPropertyName("login")]
    public string Login { get; init; } = null!;

    [JsonPropertyName("mode")]
    public ProfileModeResponse Mode { get; init; }

    [JsonPropertyName("owned")]
    public bool Owned { get; init; }

    [JsonPropertyName("content")]
    public JsonElement Content { get; init; }
}

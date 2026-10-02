namespace Contracts.Dto;

public sealed record JettonMetadataResponse
{
    [JsonPropertyName("minter_addr")]
    public string MinterAddr { get; init; } = "";
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    [JsonPropertyName("symbol")]
    public string? Symbol { get; init; }
    [JsonPropertyName("decimals")]
    public byte Decimals { get; init; }
}

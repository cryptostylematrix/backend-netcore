namespace Contracts.Dto;

public sealed class DeployPlaceBodyResponse
{
    [JsonPropertyName("boc_hex")]
    public string BocHex { get; init; } = null!;
}
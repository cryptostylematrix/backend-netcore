namespace Contracts.Dto;

public sealed class DeployItemBodyResponse
{
    [JsonPropertyName("boc_hex")]
    public string BocHex { get; init; } = null!;
}
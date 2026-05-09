namespace Contracts.Dto;

public sealed class ChooseInviterBodyResponse
{
    [JsonPropertyName("boc_hex")]
    public string BocHex { get; init; } = null!;
}
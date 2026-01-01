namespace Contracts.Dto;

public sealed class EditContentBodyResponse
{
    [JsonPropertyName("boc_hex")]
    public string BocHex { get; init; } = null!;
}
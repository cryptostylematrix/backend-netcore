namespace Contracts.Dto;

public sealed class JettonTransferMsgBodyResponse
{
    [JsonPropertyName("boc_hex")]
    public string BocHex { get; init; } = null!;
}

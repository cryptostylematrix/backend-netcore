namespace Contracts.Presentation.Endpoints.JettonWallet.BuildTransferMsgBody;

public sealed class BuildTransferMsgBodyRequest
{
    [BindFrom("query_id")]
    public ulong QueryId { get; init; }

    [BindFrom("amount")]
    public ulong Amount { get; init; }

    [BindFrom("destination_addr")]
    public string DestinationAddr { get; init; } = null!;

    [BindFrom("response_destination_addr")]
    public string? ResponseDestinationAddr { get; init; }

    [BindFrom("custom_payload_boc_hex")]
    public string? CustomPayloadBocHex { get; init; }

    [BindFrom("forward_ton_amount")]
    public ulong ForwardTonAmount { get; init; }

    [BindFrom("forward_payload_boc_hex")]
    public string? ForwardPayloadBocHex { get; init; }
}

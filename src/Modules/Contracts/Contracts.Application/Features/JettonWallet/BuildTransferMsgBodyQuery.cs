namespace Contracts.Application.Features.JettonWallet;

public sealed record BuildTransferMsgBodyQuery(
    ulong QueryId,
    ulong Amount,
    string DestinationAddr,
    string? ResponseDestinationAddr,
    string? CustomPayloadBocHex,
    ulong ForwardTonAmount,
    string? ForwardPayloadBocHex) : IQuery<JettonTransferMsgBodyResponse>;

internal sealed class BuildTransferMsgBodyQueryHandler(IJettonWalletQueries queries)
    : IQueryHandler<BuildTransferMsgBodyQuery, JettonTransferMsgBodyResponse>
{
    public Task<Result<JettonTransferMsgBodyResponse>> Handle(
        BuildTransferMsgBodyQuery request,
        CancellationToken ct) => Task.FromResult(queries.BuildTransferMsgBody(
            request.QueryId,
            request.Amount,
            request.DestinationAddr.Trim(),
            request.ResponseDestinationAddr?.Trim(),
            request.CustomPayloadBocHex?.Trim(),
            request.ForwardTonAmount,
            request.ForwardPayloadBocHex?.Trim()));
}

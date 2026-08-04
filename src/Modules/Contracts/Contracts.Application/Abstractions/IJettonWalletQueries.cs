namespace Contracts.Application.Abstractions;

public interface IJettonWalletQueries
{
    Result<JettonTransferMsgBodyResponse> BuildTransferMsgBody(
        ulong queryId,
        ulong amount,
        string destinationAddr,
        string? responseDestinationAddr,
        string? customPayloadBocHex,
        ulong forwardTonAmount,
        string? forwardPayloadBocHex);

    Task<Result<JettonWalletDataResponse>> GetWalletDataAsync(
        string addr, 
        CancellationToken ct = default);
}

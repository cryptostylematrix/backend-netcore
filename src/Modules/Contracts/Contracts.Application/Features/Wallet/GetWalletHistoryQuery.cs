namespace Contracts.Application.Features.Wallet;

public sealed record GetWalletHistoryQuery(
    string Addr,
    uint Limit,
    ulong? Lt,
    string? Hash) : IQuery<WalletTransactionHistoryResponse>;

internal sealed class GetWalletHistoryQueryHandler(IWalletQueries walletQueries)
    : IQueryHandler<GetWalletHistoryQuery, WalletTransactionHistoryResponse>
{
    public Task<Result<WalletTransactionHistoryResponse>> Handle(GetWalletHistoryQuery request, CancellationToken ct)
        => walletQueries.GetWalletHistoryAsync(
            addr: request.Addr,
            limit: request.Limit,
            lt: request.Lt,
            hash: request.Hash,
            ct: ct);
}
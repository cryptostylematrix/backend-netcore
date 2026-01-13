namespace Contracts.Application.Abstractions;




public interface IWalletQueries
{
    Task<Result<TransactionHistoryResponse>> GetWalletHistoryAsync(
        string addr, 
        uint limit,
        ulong? lt,
        string? hash,
        CancellationToken ct = default);
}
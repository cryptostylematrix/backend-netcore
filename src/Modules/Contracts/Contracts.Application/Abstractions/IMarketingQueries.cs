namespace Contracts.Application.Abstractions;

public interface IMarketingQueries
{
    Task<Result<MarketingTransactionHistoryResponse>> GetMarketingHistoryAsync(
        string addr, 
        uint limit,
        ulong? lt,
        string? hash,
        CancellationToken ct = default);
}
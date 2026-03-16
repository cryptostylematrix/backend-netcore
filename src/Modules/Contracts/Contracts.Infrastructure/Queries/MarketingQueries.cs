namespace Contracts.Infrastructure.Queries;

public sealed class MarketingQueries(ITonClient tonClient) : IMarketingQueries
{
    public async Task<Result<MarketingTransactionHistoryResponse>> GetMarketingHistoryAsync(
        string addr, 
        uint limit,
        ulong? lt,
        string? hash,
        CancellationToken ct = default)
    {
        try
        {
            var result = await tonClient.GetTransactions(
                address: new Address(addr),
                limit: limit,
                lt: lt,
                hash: hash,
                to_lt: null,
                archival: true);

            if (result is null)
                return Result<MarketingTransactionHistoryResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));
            
            var response = new MarketingTransactionHistoryResponse
            {
                Items = result.Select(tr => new MarketingTransactionResponse
                {
                    Lt = tr.TransactionId.Lt,
                    Hash = tr.TransactionId.Hash,
                    UTime = tr.UTime,
                    Messages = MarkeetingTransactionMessageFactory.Create(tr)
                }).ToArray(),
            };
        
            return Result.Success(response);
        }
        catch (Exception exc)
        {
            return Result<MarketingTransactionHistoryResponse>.Error(exc.Message);
        }
    }
}
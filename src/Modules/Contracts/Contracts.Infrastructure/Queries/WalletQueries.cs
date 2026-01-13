namespace Contracts.Infrastructure.Queries;

public sealed class WalletQueries(ITonClient tonClient) : IWalletQueries
{
    public async Task<Result<TransactionHistoryResponse>> GetWalletHistoryAsync(
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
                return Result<TransactionHistoryResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));
            
            var response = new TransactionHistoryResponse
            {
                Items = result.Select(tr => new TransactionResponse
                {
                    Lt = tr.TransactionId.Lt,
                    Hash = tr.TransactionId.Hash,
                    UTime = tr.UTime,
                    Messages = TransactionMessageFactory.Create(tr)
                }).ToArray(),
            };
        
            return Result.Success(response);
        }
        catch (Exception exc)
        {
            return Result<TransactionHistoryResponse>.Error(exc.Message);
        }
    }
}
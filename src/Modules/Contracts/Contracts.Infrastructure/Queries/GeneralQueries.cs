namespace Contracts.Infrastructure.Queries;

public sealed class GeneralQueries(ITonClient tonClient) : IGeneralQueries
{
    public async Task<Result<ContractBalanceResponse>> GetContractBalanceAsync(string addr, CancellationToken ct = default)
    {
        try
        {
            // exactly what MultiContract did, but build DTOs
            var result = await tonClient.GetBalance(new Address(addr));

            if (result is null)
                return Result<ContractBalanceResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            return Result.Success(new ContractBalanceResponse
            {
                Balance = result.ToDecimal()
            });
        }
        catch (Exception exc)
        {
            return Result<ContractBalanceResponse>.Error(exc.Message);
        }
    }
}
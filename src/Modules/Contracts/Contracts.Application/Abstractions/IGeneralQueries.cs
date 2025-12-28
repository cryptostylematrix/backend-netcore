namespace Contracts.Application.Abstractions;

public interface IGeneralQueries
{
    Task<Result<ContractBalanceResponse>> GetContractBalanceAsync(string addr, CancellationToken ct = default);
}
namespace Contracts.Application.Abstractions;

public interface IJettonWalletQueries
{
    Task<Result<JettonWalletDataResponse>> GetWalletDataAsync(
        string addr, 
        CancellationToken ct = default);
}
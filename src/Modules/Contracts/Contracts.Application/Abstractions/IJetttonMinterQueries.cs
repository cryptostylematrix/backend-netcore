namespace Contracts.Application.Abstractions;

public interface IJetttonMinterQueries
{
    Task<Result<JettonWalletAddressResponse>> GetWalletAddressAsync(
        string addr, 
        string ownerAddr,
        CancellationToken ct = default);

    Task<Result<JettonMinterDataResponse>> GetJettonDataAsync(
        string addr,
        CancellationToken ct = default);
}

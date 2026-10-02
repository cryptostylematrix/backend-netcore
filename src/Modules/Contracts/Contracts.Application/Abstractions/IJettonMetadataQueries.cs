namespace Contracts.Application.Abstractions;

public interface IJettonMetadataQueries
{
    Task<Result<JettonMetadataResponse>> GetByWalletAsync(string address, CancellationToken ct);
}

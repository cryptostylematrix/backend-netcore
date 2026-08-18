namespace UI.Application.Abstractions;

public interface IWalletProfileQueries
{
    Task<IReadOnlyCollection<WalletProfileResponse>> ListAsync(
        string walletAddr,
        CancellationToken cancellationToken);
}

using Common.Domain;

namespace UI.Core.WalletProfileIntentAggregate;

public interface IWalletProfileIntentRepository : IRepository<WalletProfileIntent>
{
    Task<WalletProfileIntent?> GetAsync(
        string walletAddr,
        string profileAddr,
        CancellationToken cancellationToken);

    Task<WalletProfileIntent?> GetByLoginAsync(
        string walletAddr,
        string profileLogin,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WalletProfileIntent>> ListAsync(
        string walletAddr,
        CancellationToken cancellationToken);

    void Add(WalletProfileIntent intent);
    void Remove(WalletProfileIntent intent);
}

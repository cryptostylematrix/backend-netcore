using Microsoft.EntityFrameworkCore;
using UI.Application.Abstractions;
using UI.Core.WalletProfileIntentAggregate;
using UI.Infrastructure.Persistence;

namespace UI.Infrastructure.Repositories;

internal sealed class WalletProfileIntentRepository(
    DataContext dataContext,
    IWalletAddressService walletAddressService)
    : IWalletProfileIntentRepository
{
    public Task<WalletProfileIntent?> GetAsync(
        string walletAddr,
        string profileAddr,
        CancellationToken cancellationToken)
    {
        var walletAddrs = EquivalentAddresses(walletAddr);
        return dataContext.WalletProfileIntents.SingleOrDefaultAsync(
            intent => walletAddrs.Contains(intent.WalletAddr)
                && intent.ProfileAddr == profileAddr,
            cancellationToken);
    }

    public Task<WalletProfileIntent?> GetByLoginAsync(
        string walletAddr,
        string profileLogin,
        CancellationToken cancellationToken)
    {
        var walletAddrs = EquivalentAddresses(walletAddr);
        return (from intent in dataContext.WalletProfileIntents
                join profile in dataContext.Profiles
                    on intent.ProfileAddr equals profile.Address
                where walletAddrs.Contains(intent.WalletAddr)
                    && profile.Login == profileLogin
                select intent).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WalletProfileIntent>> ListAsync(
        string walletAddr,
        CancellationToken cancellationToken)
    {
        var walletAddrs = EquivalentAddresses(walletAddr);
        return await dataContext.WalletProfileIntents
            .Where(intent => walletAddrs.Contains(intent.WalletAddr))
            .OrderBy(intent => intent.Id)
            .ToArrayAsync(cancellationToken);
    }

    public void Add(WalletProfileIntent intent) =>
        dataContext.WalletProfileIntents.Add(intent);

    public void Remove(WalletProfileIntent intent) =>
        dataContext.WalletProfileIntents.Remove(intent);

    private string[] EquivalentAddresses(string walletAddr) =>
        walletAddressService.GetEquivalentRepresentations(walletAddr).ToArray();
}

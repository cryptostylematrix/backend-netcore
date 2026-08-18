using UI.Core.WalletProfileIntentAggregate;
using UI.Infrastructure.Persistence;

namespace UI.Infrastructure.Repositories;

internal sealed class WalletProfileIntentEventRepository(DataContext dataContext)
    : IWalletProfileIntentEventRepository
{
    public void Add(WalletProfileIntentEvent eventItem) =>
        dataContext.WalletProfileIntentEvents.Add(eventItem);
}

namespace UI.Core.WalletProfileIntentAggregate;

public interface IWalletProfileIntentEventRepository
{
    void Add(WalletProfileIntentEvent eventItem);
}

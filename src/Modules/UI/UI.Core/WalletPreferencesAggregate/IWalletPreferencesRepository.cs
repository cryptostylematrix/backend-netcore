namespace UI.Core.WalletPreferencesAggregate;

public interface IWalletPreferencesRepository
{
    // Atomic initialization keeps an existing preference; explicit selection may replace it.
    Task<string> SaveLanguageAsync(WalletPreferences preferences, bool overwrite,
        CancellationToken cancellationToken);
}

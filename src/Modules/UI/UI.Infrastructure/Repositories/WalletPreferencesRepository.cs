using Dapper;
using Microsoft.EntityFrameworkCore;
using UI.Core.WalletPreferencesAggregate;
using UI.Infrastructure.Persistence;

namespace UI.Infrastructure.Repositories;

internal sealed class WalletPreferencesRepository(DataContext context) : IWalletPreferencesRepository
{
    public async Task<string> SaveLanguageAsync(WalletPreferences preferences, bool overwrite,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO public.wallet_preferences (wallet_addr, language, updated_at)
            VALUES (@WalletAddr, @Language, @UpdatedAtUtc)
            ON CONFLICT (wallet_addr) DO UPDATE SET
                language = CASE WHEN @overwrite THEN EXCLUDED.language ELSE wallet_preferences.language END,
                updated_at = CASE WHEN @overwrite AND wallet_preferences.language IS DISTINCT FROM EXCLUDED.language
                    THEN EXCLUDED.updated_at ELSE wallet_preferences.updated_at END
            RETURNING language;
            """;
        var language = await context.Database.GetDbConnection().ExecuteScalarAsync<string>(
            new CommandDefinition(sql, new { preferences.WalletAddr, preferences.Language,
                preferences.UpdatedAtUtc, overwrite }, cancellationToken: cancellationToken));
        return language ?? throw new InvalidOperationException("Wallet language was not returned after upsert.");
    }
}

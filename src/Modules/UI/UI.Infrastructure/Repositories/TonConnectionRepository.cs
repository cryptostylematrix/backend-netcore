using Microsoft.EntityFrameworkCore;
using UI.Core.TonConnectionAggregate;
using UI.Infrastructure.Persistence;

namespace UI.Infrastructure.Repositories;

internal sealed class TonConnectionRepository(DataContext dataContext) : ITonConnectionRepository
{
    public async Task UpsertAsync(TonConnection connection, CancellationToken cancellationToken)
    {
        // One atomic repository write: no read/insert race between tabs or devices.
        await dataContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.ton_connections
                (wallet_addr, contract_version, wallet_name, app_version, platform, created_at, updated_at, last_connected_at)
            VALUES ({connection.WalletAddr}, {connection.ContractVersion}, {connection.WalletName},
                {connection.AppVersion}, {connection.Platform}, {connection.CreatedAtUtc}, {connection.UpdatedAtUtc},
                {connection.LastConnectedAtUtc})
            ON CONFLICT (wallet_addr) DO UPDATE SET
                contract_version = EXCLUDED.contract_version,
                wallet_name = EXCLUDED.wallet_name,
                app_version = EXCLUDED.app_version,
                platform = EXCLUDED.platform,
                updated_at = CASE WHEN
                    (ton_connections.contract_version, ton_connections.wallet_name,
                     ton_connections.app_version, ton_connections.platform)
                    IS DISTINCT FROM
                    (EXCLUDED.contract_version, EXCLUDED.wallet_name, EXCLUDED.app_version, EXCLUDED.platform)
                    THEN EXCLUDED.updated_at ELSE ton_connections.updated_at END,
                last_connected_at = GREATEST(ton_connections.last_connected_at, EXCLUDED.last_connected_at)
            """, cancellationToken);
    }
}

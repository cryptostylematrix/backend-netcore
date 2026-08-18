using System.Text.Json;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using UI.Application.Abstractions;
using UI.Dto;

namespace UI.Infrastructure.Queries;

internal sealed class WalletProfileQueries(
    [FromKeyedServices("UI")] NpgsqlDataSource dataSource,
    IWalletAddressService walletAddressService)
    : IWalletProfileQueries
{
    public async Task<IReadOnlyCollection<WalletProfileResponse>> ListAsync(
        string walletAddr,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                @walletAddr            AS "WalletAddr",
                intent.profile_addr    AS "ProfileAddr",
                profile.login          AS "Login",
                intent.mode            AS "Mode",
                intent.owned           AS "Owned",
                profile.content::text  AS "ContentJson"
            FROM public.wallet_profile_intents intent
            JOIN public.profiles profile
              ON profile.address = intent.profile_addr
            WHERE intent.wallet_addr = ANY(@walletAddrs)
            ORDER BY intent.id;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<WalletProfileRow>(
            new CommandDefinition(
                sql,
                new
                {
                    walletAddr,
                    walletAddrs = walletAddressService
                        .GetEquivalentRepresentations(walletAddr)
                        .ToArray()
                },
                cancellationToken: cancellationToken));

        return rows.Select(row => new WalletProfileResponse
        {
            WalletAddr = row.WalletAddr,
            ProfileAddr = row.ProfileAddr,
            Login = row.Login,
            Mode = Enum.Parse<ProfileModeResponse>(row.Mode, ignoreCase: true),
            Owned = row.Owned,
            Content = JsonSerializer.Deserialize<JsonElement>(row.ContentJson)
        }).ToArray();
    }

    private sealed class WalletProfileRow
    {
        public string WalletAddr { get; init; } = null!;
        public string ProfileAddr { get; init; } = null!;
        public string Login { get; init; } = null!;
        public string Mode { get; init; } = null!;
        public bool Owned { get; init; }
        public string ContentJson { get; init; } = null!;
    }
}

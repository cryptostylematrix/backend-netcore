using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Dto;

namespace ReferalProgram.Infrastructure.Queries;

internal sealed class ProfileVolumeQueries(
    [FromKeyedServices("Programs")] NpgsqlDataSource dataSource)
    : IProfileVolumeQueries
{
    public async Task<ProfileVolumeResponse> GetAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                @marketingAddr                 AS "MarketingAddr",
                @structureNumber               AS "StructureNumber",
                @profileAddr                   AS "ProfileAddr",
                COALESCE(personal_volume, 0)   AS "PersonalVolume",
                COALESCE(referral_volume, 0)   AS "ReferralVolume",
                COALESCE(group_volume, 0)      AS "GroupVolume"
            FROM (SELECT 1) AS requested
            LEFT JOIN public.profile_volumes volume
              ON volume.marketing_addr = @marketingAddr
             AND volume.structure_number = @structureNumber
             AND volume.profile_addr = @profileAddr;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<ProfileVolumeResponse>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber,
                    profileAddr
                },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyDictionary<string, uint>> GetReferralVolumesAsync(
        string marketingAddr,
        byte structureNumber,
        IReadOnlyCollection<string> profileAddresses,
        CancellationToken cancellationToken)
    {
        if (profileAddresses.Count == 0)
            return new Dictionary<string, uint>(StringComparer.Ordinal);

        const string sql = """
            SELECT
                requested.profile_addr AS "ProfileAddr",
                COALESCE(volume.referral_volume, 0) AS "ReferralVolume"
            FROM unnest(@profileAddresses) AS requested(profile_addr)
            LEFT JOIN public.profile_volumes volume
              ON volume.marketing_addr = @marketingAddr
             AND volume.structure_number = @structureNumber
             AND volume.profile_addr = requested.profile_addr;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ReferralVolumeRow>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber,
                    profileAddresses = profileAddresses.Distinct(StringComparer.Ordinal).ToArray()
                },
                cancellationToken: cancellationToken));

        return rows.ToDictionary(
            row => row.ProfileAddr,
            row => row.ReferralVolume,
            StringComparer.Ordinal);
    }

    private sealed class ReferralVolumeRow
    {
        public string ProfileAddr { get; init; } = null!;
        public uint ReferralVolume { get; init; }
    }
}

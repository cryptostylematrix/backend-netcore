using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Dto;

namespace ReferalProgram.Infrastructure.Queries;

public sealed class ProgramStatisticsQueries(
    [FromKeyedServices("Programs")] NpgsqlDataSource dataSource)
    : IProgramStatisticsQueries
{
    public async Task<ProgramStatisticsResponse?> GetAsync(
        string marketingAddr,
        string profileAddr,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS
            (
                SELECT 1
                FROM public.places
                WHERE marketing_addr = @marketingAddr
                  AND structure_number = 0
                  AND profile_addr = @profileAddr
                  AND place_number = 1
            );

            SELECT
                COUNT(*)::bigint AS "Total",
                COUNT(*) FILTER (WHERE is_active)::bigint AS "Active"
            FROM public.places
            WHERE marketing_addr = @marketingAddr
              AND structure_number = 0
              AND parent_profile_addr = @profileAddr
              AND profile_addr IS NOT NULL;

            WITH direct_referrals AS
            (
                SELECT DISTINCT profile_addr
                FROM public.places
                WHERE marketing_addr = @marketingAddr
                  AND structure_number = 0
                  AND parent_profile_addr = @profileAddr
                  AND profile_addr IS NOT NULL
            ),
            structure_places AS
            (
                SELECT
                    structure_number,
                    COUNT(*)::bigint AS total_places,
                    COUNT(*) FILTER (WHERE is_active)::bigint AS active_places
                FROM public.places
                WHERE marketing_addr = @marketingAddr
                GROUP BY structure_number
            ),
            referral_places AS
            (
                SELECT
                    place.structure_number,
                    COUNT(DISTINCT place.profile_addr)::bigint AS total_referrals,
                    COUNT(DISTINCT place.profile_addr)
                        FILTER (WHERE place.is_active)::bigint AS active_referrals,
                    COUNT(*)::bigint AS total_places,
                    COUNT(*) FILTER (WHERE place.is_active)::bigint AS active_places
                FROM public.places place
                JOIN direct_referrals referral
                  ON referral.profile_addr = place.profile_addr
                WHERE place.marketing_addr = @marketingAddr
                GROUP BY place.structure_number
            )
            SELECT
                structure.structure_number AS "StructureNumber",
                COALESCE(all_places.total_places, 0)::bigint AS "TotalPlaces",
                COALESCE(all_places.active_places, 0)::bigint AS "ActivePlaces",
                COALESCE(referral_places.total_referrals, 0)::bigint AS "ReferralTotal",
                COALESCE(referral_places.active_referrals, 0)::bigint AS "ReferralActive",
                (
                    COALESCE(referral_places.total_referrals, 0)
                    - COALESCE(referral_places.active_referrals, 0)
                )::bigint AS "ReferralInactive",
                COALESCE(referral_places.total_places, 0)::bigint AS "ReferralTotalPlaces",
                COALESCE(referral_places.active_places, 0)::bigint AS "ReferralActivePlaces"
            FROM public.structures structure
            LEFT JOIN structure_places all_places
              ON all_places.structure_number = structure.structure_number
            LEFT JOIN referral_places
              ON referral_places.structure_number = structure.structure_number
            WHERE structure.marketing_addr = @marketingAddr
            ORDER BY structure.structure_number;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(
                sql,
                new { marketingAddr, profileAddr },
                cancellationToken: cancellationToken));

        if (!await results.ReadSingleAsync<bool>())
            return null;

        var referrals = await results.ReadSingleAsync<ReferralCountRow>();
        var structures = (await results.ReadAsync<StructureStatisticsRow>())
            .Select(row => new StructureStatisticsResponse
            {
                StructureNumber = checked((byte)row.StructureNumber),
                TotalPlaces = row.TotalPlaces,
                ActivePlaces = row.ActivePlaces,
                Referrals = new StructureReferralStatisticsResponse
                {
                    Total = row.ReferralTotal,
                    Active = row.ReferralActive,
                    Inactive = row.ReferralInactive,
                    TotalPlaces = row.ReferralTotalPlaces,
                    ActivePlaces = row.ReferralActivePlaces
                }
            })
            .ToArray();

        return new ProgramStatisticsResponse
        {
            MarketingAddr = marketingAddr,
            ProfileAddr = profileAddr,
            Referrals = new ReferralCountStatisticsResponse
            {
                Total = referrals.Total,
                Active = referrals.Active,
                Inactive = referrals.Total - referrals.Active
            },
            Structures = structures
        };
    }

    private sealed class ReferralCountRow
    {
        public long Total { get; init; }
        public long Active { get; init; }
    }

    private sealed class StructureStatisticsRow
    {
        public short StructureNumber { get; init; }
        public long TotalPlaces { get; init; }
        public long ActivePlaces { get; init; }
        public long ReferralTotal { get; init; }
        public long ReferralActive { get; init; }
        public long ReferralInactive { get; init; }
        public long ReferralTotalPlaces { get; init; }
        public long ReferralActivePlaces { get; init; }
    }
}

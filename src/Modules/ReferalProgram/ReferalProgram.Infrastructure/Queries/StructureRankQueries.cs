using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Dto;

namespace ReferalProgram.Infrastructure.Queries;

public sealed class StructureRankQueries(
    [FromKeyedServices("Programs")] NpgsqlDataSource dataSource) : IStructureRankQueries
{
    public async Task<IReadOnlyCollection<StructureRankResponse>> GetAllAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                marketing_addr                  AS "MarketingAddr",
                structure_number                AS "StructureNumber",
                name                            AS "Name",
                required_active_referral_places AS "RequiredActiveReferralPlaces"
            FROM public.structure_ranks
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
            ORDER BY required_active_referral_places, name;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<StructureRankRow>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber
                },
                cancellationToken: cancellationToken));

        return rows
            .Select(row => new StructureRankResponse
            {
                MarketingAddr = row.MarketingAddr,
                StructureNumber = checked((byte)row.StructureNumber),
                Name = row.Name,
                RequiredActiveReferralPlaces = checked((uint)row.RequiredActiveReferralPlaces)
            })
            .ToArray();
    }

    private sealed class StructureRankRow
    {
        public string MarketingAddr { get; init; } = null!;
        public short StructureNumber { get; init; }
        public string Name { get; init; } = null!;
        public long RequiredActiveReferralPlaces { get; init; }
    }
}

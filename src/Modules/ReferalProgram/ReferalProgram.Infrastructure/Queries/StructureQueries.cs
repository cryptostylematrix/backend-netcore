using System.Text.Json;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Dto;

namespace ReferalProgram.Infrastructure.Queries;

public sealed class StructureQueries(
    [FromKeyedServices("Programs")] NpgsqlDataSource dataSource) : IStructureQueries, IProgramStructureListQueries
{
    public async Task<StructureResponse?> GetStructureAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<StructureRow>(
            new CommandDefinition(
                SelectSql + " WHERE marketing_addr = @marketingAddr AND structure_number = @structureNumber;",
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber
                },
                cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<StructureResponse>> GetAsync(string marketingAddress, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<StructureRow>(new CommandDefinition(
            SelectSql + " WHERE marketing_addr = @marketingAddress ORDER BY structure_number;",
            new { marketingAddress }, cancellationToken: ct));
        return rows.Select(Map).ToArray();
    }

    private static StructureResponse Map(StructureRow row) => new()
    {
        MarketingAddr = row.MarketingAddr,
        StructureNumber = checked((byte)row.StructureNumber),
        MaxPlacesPerProfile = row.MaxPlacesPerProfile,
        Width = checked((byte)row.Width),
        Height = checked((byte)row.Height),
        DisplayHeight = checked((byte)row.DisplayHeight),
        PrevRequired = row.PrevRequired,
        Group = row.Group,
        PosAlgo = JsonSerializer.Deserialize<JsonElement>(row.PosAlgoJson),
        Activity = row.ActivityJson is null
            ? null
            : JsonSerializer.Deserialize<JsonElement>(row.ActivityJson)
    };

    private const string SelectSql = """
            SELECT
                marketing_addr          AS "MarketingAddr",
                structure_number        AS "StructureNumber",
                max_places_per_profile  AS "MaxPlacesPerProfile",
                width                   AS "Width",
                height                  AS "Height",
                display_height          AS "DisplayHeight",
                prev_required           AS "PrevRequired",
                pos_algo::text          AS "PosAlgoJson",
                activity::text          AS "ActivityJson",
                "group"                 AS "Group"
            FROM public.structures
            """;

    private sealed class StructureRow
    {
        public string MarketingAddr { get; init; } = null!;
        public short StructureNumber { get; init; }
        public int MaxPlacesPerProfile { get; init; }
        public short Width { get; init; }
        public short Height { get; init; }
        public short DisplayHeight { get; init; }
        public bool PrevRequired { get; init; }
        public string PosAlgoJson { get; init; } = null!;
        public string? ActivityJson { get; init; }
        public string? Group { get; init; }
    }
}

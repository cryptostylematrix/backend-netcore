using System.Text.Json;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Dto;

namespace ReferalProgram.Infrastructure.Queries;

public sealed class StructureQueries(
    [FromKeyedServices("Programs")] NpgsqlDataSource dataSource) : IStructureQueries
{
    public async Task<StructureResponse?> GetStructureAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                marketing_addr          AS "MarketingAddr",
                structure_number        AS "StructureNumber",
                max_places_per_profile  AS "MaxPlacesPerProfile",
                width                   AS "Width",
                height                  AS "Height",
                display_height          AS "DisplayHeight",
                prev_required           AS "PrevRequired",
                pos_algo::text          AS "PosAlgoJson",
                activity::text          AS "ActivityJson"
            FROM public.structures
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<StructureRow>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber
                },
                cancellationToken: cancellationToken));

        return row is null
            ? null
            : new StructureResponse
            {
                MarketingAddr = row.MarketingAddr,
                StructureNumber = checked((byte)row.StructureNumber),
                MaxPlacesPerProfile = row.MaxPlacesPerProfile,
                Width = checked((byte)row.Width),
                Height = checked((byte)row.Height),
                DisplayHeight = checked((byte)row.DisplayHeight),
                PrevRequired = row.PrevRequired,
                PosAlgo = JsonSerializer.Deserialize<JsonElement>(row.PosAlgoJson),
                Activity = row.ActivityJson is null
                    ? null
                    : JsonSerializer.Deserialize<JsonElement>(row.ActivityJson)
            };
    }

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
    }
}

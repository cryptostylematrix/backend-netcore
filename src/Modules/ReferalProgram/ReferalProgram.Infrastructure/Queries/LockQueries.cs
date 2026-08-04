using Common.Dto;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Dto;

namespace ReferalProgram.Infrastructure.Queries;

public sealed class LockQueries(
    [FromKeyedServices("Programs")] NpgsqlDataSource dataSource) : ILockQueries
{
    public async Task<Paginated<LockResponse>> GetLocksAsync(
        string marketingAddr,
        byte structNumber,
        string profileAddr,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? pageSize : 20;
        var offset = (safePage - 1) * safePageSize;
        var parameters = new
        {
            marketingAddr,
            structNumber = (short)structNumber,
            profileAddr,
            limit = safePageSize,
            offset
        };

        await using var connection = await dataSource.OpenConnectionAsync(ct);

        const string countSql = """
            SELECT COUNT(*)::bigint
            FROM public.locks
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structNumber
              AND profile_addr = @profileAddr;
            """;

        var total = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        if (total == 0)
        {
            return new Paginated<LockResponse>
            {
                Items = Array.Empty<LockResponse>(),
                Page = safePage,
                TotalPages = 1
            };
        }

        const string dataSql = """
            SELECT
                marketing_addr      AS "MarketingAddr",
                structure_number    AS "StructNumber",
                place_profile_addr  AS "ProfileAddr",
                place_number        AS "PlaceNumber",
                place_profile_login AS "PlaceProfileLogin",
                locked_pos          AS "LockedPos",
                created_at          AS "CreatedAt"
            FROM public.locks
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structNumber
              AND profile_addr = @profileAddr
            ORDER BY created_at ASC, id ASC
            LIMIT @limit OFFSET @offset;
            """;

        var items = (await connection.QueryAsync<LockResponse>(
            new CommandDefinition(dataSql, parameters, cancellationToken: ct)))
            .AsList();

        return new Paginated<LockResponse>
        {
            Items = items,
            Page = safePage,
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)total / safePageSize))
        };
    }
}

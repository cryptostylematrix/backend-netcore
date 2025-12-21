using System.Data;
using Dapper;
using Npgsql;

namespace Matrix.Infrastructure.Data.Repositories;


public sealed class PlaceReadOnlyRepository(NpgsqlDataSource dataSource) : IPlaceReadOnlyRepository
{
    private const string SelectColumns = """
                                                 id,
                                                 parent_id            AS ParentId,
                                                 task_key             AS TaskKey,
                                                 task_query_id        AS TaskQueryId,
                                                 task_source_addr     AS TaskSourceAddr,
                                                 m                    AS M,
                                                 mp                   AS Mp,
                                                 pos                  AS Pos,
                                                 addr                 AS Addr,
                                                 profile_addr         AS ProfileAddr,
                                                 inviter_profile_addr AS InviterProfileAddr,
                                                 parent_addr          AS ParentAddr,
                                                 place_number         AS PlaceNumber,
                                                 craeted_at           AS CreatedAt,   -- NOTE: matches your existing DB typo
                                                 filling              AS Filling,
                                                 filling2             AS Filling2,
                                                 clone                AS Clone,
                                                 profile_login        AS ProfileLogin,
                                                 "index"              AS "Index",
                                                 confirmed            AS Confirmed
                                         """;
    
    private static async Task<long> GetPlacesCountInternal(IDbConnection conn, short m, string profileAddr)
    {
        const string sql = """
                                SELECT COUNT(*)::bigint
                                FROM multi_places
                                WHERE m = @m AND profile_addr = @profileAddr;
                            """;

        var count = await conn.ExecuteScalarAsync<long>(sql, new { m, profileAddr });
        return count;
    }

    private static async Task<string?> GetRootMpInternal(IDbConnection conn, short m, string profileAddr)
    {
        const string sql = """
                               SELECT mp
                               FROM multi_places
                               WHERE m = @m AND profile_addr = @profileAddr AND place_number = 1
                               LIMIT 1;
                           """;

        return await conn.QuerySingleOrDefaultAsync<string?>(sql, new { m, profileAddr });
    }

    public async Task<PlacesResult> GetPlaces(short m, string profileAddr, int page, int pageSize)
    {
        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? pageSize : 10;
        var offset = (safePage - 1) * safePageSize;

        await using var conn = await dataSource.OpenConnectionAsync();

        var total = await GetPlacesCountInternal(conn, m, profileAddr);
        if (total == 0) return new PlacesResult(Array.Empty<MultiPlace>(), 0);

        const string sql = $"""
                                        SELECT {SelectColumns}
                                        FROM multi_places
                                        WHERE m = @m AND profile_addr = @profileAddr
                                        ORDER BY place_number ASC
                                        LIMIT @limit OFFSET @offset;
                            """;

        var items = (await conn.QueryAsync<MultiPlace>(sql, new
        {
            m,
            profileAddr,
            limit = safePageSize,
            offset
        })).ToArray();

        return new PlacesResult(items, total);
    }

    public async Task<long> GetPlacesCount(short m, string profileAddr)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        return await GetPlacesCountInternal(conn, m, profileAddr);
    }

    public async Task<MultiPlace?> GetRootPlace(short m, string profileAddr)
    {
        await using var conn = await dataSource.OpenConnectionAsync();

        const string sql = $"""
                                SELECT {SelectColumns}
                                FROM multi_places
                                WHERE m = @m AND profile_addr = @profileAddr AND place_number = 1
                                LIMIT 1;
                            """;

        return await conn.QuerySingleOrDefaultAsync<MultiPlace?>(sql, new { m, profileAddr });
    }

    public async Task<MultiPlace?> GetPlaceByTaskKey(int taskKey)
    {
        await using var conn = await dataSource.OpenConnectionAsync();

        const string sql = $"""
                                        SELECT {SelectColumns}
                                        FROM multi_places
                                        WHERE task_key = @taskKey
                                        LIMIT 1;
                            """;

        return await conn.QuerySingleOrDefaultAsync<MultiPlace?>(sql, new { taskKey });
    }

    public async Task<int> GetMaxPlaceNumber(short m, string profileAddr)
    {
        await using var conn = await dataSource.OpenConnectionAsync();

        const string sql = """
                                       SELECT COALESCE(MAX(place_number), 0)
                                       FROM multi_places
                                       WHERE m = @m AND profile_addr = @profileAddr;
                           """;

        return await conn.ExecuteScalarAsync<int>(sql, new { m, profileAddr });
    }

    public async Task<PlacesResult> SearchPlaces(short m, string profileAddr, string query, int page, int pageSize)
    {
        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? pageSize : 10;
        var offset = (safePage - 1) * safePageSize;

        await using var conn = await dataSource.OpenConnectionAsync();

        var rootMp = await GetRootMpInternal(conn, m, profileAddr);
        if (string.IsNullOrWhiteSpace(rootMp))
            return new PlacesResult(Array.Empty<MultiPlace>(), 0);

        var mpPrefix = rootMp + "%";
        var indexPrefix = (query ?? string.Empty) + "%";

        const string countSql = """
                                            SELECT COUNT(*)::bigint
                                            FROM multi_places
                                            WHERE m = @m AND mp LIKE @mpPrefix AND "index" LIKE @indexPrefix;
                                """;

        var total = (uint)await conn.ExecuteScalarAsync<long>(countSql, new { m, mpPrefix, indexPrefix });
        if (total == 0) return new PlacesResult(Array.Empty<MultiPlace>(), 0);

        const string sql = $"""
                                        SELECT {SelectColumns}
                                        FROM multi_places
                                        WHERE m = @m AND mp LIKE @mpPrefix AND "index" LIKE @indexPrefix
                                        ORDER BY "index" ASC
                                        LIMIT @limit OFFSET @offset;
                            """;

        var items = (await conn.QueryAsync<MultiPlace>(sql, new
        {
            m,
            mpPrefix,
            indexPrefix,
            limit = safePageSize,
            offset
        })).ToArray();

        return new PlacesResult(items, total);
    }

    public async Task<MultiPlace?> GetPlaceByAddress(string addr)
    {
        await using var conn = await dataSource.OpenConnectionAsync();

        const string sql = $"""
                                        SELECT {SelectColumns}
                                        FROM multi_places
                                        WHERE addr = @addr
                                        LIMIT 1;
                            """;

        return await conn.QuerySingleOrDefaultAsync<MultiPlace?>(sql, new { addr });
    }

    public async Task<long> GetPlacesCountByMpPrefix(short m, string mpPrefix)
    {
        await using var conn = await dataSource.OpenConnectionAsync();

        const string sql = """
                                       SELECT COUNT(*)::bigint
                                       FROM multi_places
                                       WHERE m = @m AND mp LIKE @prefix;
                           """;

        var count = await conn.ExecuteScalarAsync<long>(sql, new { m, prefix = mpPrefix + "%" });
        return count > 0 ? count : 0;
    }

    public async Task<PlacesResult> GetPlacesByMpPrefix(short m, string mpPrefix, int depthLevels, int page, int pageSize)
    {
        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? pageSize : 10;
        var offset = (safePage - 1) * safePageSize;

        var maxLength = (mpPrefix?.Length ?? 0) + depthLevels;
        var prefix = mpPrefix + "%";

        await using var conn = await dataSource.OpenConnectionAsync();

        const string countSql = $"""
                                    SELECT COUNT(*)::bigint
                                    FROM multi_places
                                    WHERE m = @m AND mp LIKE @prefix AND length(mp) <= @maxLength;
                                """;

        var total = await conn.ExecuteScalarAsync<long>(countSql, new { m, prefix, maxLength });
        if (total == 0) return new PlacesResult(Array.Empty<MultiPlace>(), 0);

        const string sql = $"""
                                SELECT {SelectColumns}
                                FROM multi_places
                                WHERE m = @m AND mp LIKE @prefix AND length(mp) <= @maxLength
                                ORDER BY length(mp) ASC, mp ASC
                                LIMIT @limit OFFSET @offset;
                            """;

        var items = (await conn.QueryAsync<MultiPlace>(sql, new
        {
            m,
            prefix,
            maxLength,
            limit = safePageSize,
            offset
        })).ToArray();

        return new PlacesResult(items, total);
    }

    public async Task<MultiPlace?> GetPlaceByMp(short m, string mp)
    {
        await using var conn = await dataSource.OpenConnectionAsync();

        const string sql = $"""
                                SELECT {SelectColumns}
                                FROM multi_places
                                WHERE m = @m AND mp = @mp
                                LIMIT 1;
                            """;

        return await conn.QuerySingleOrDefaultAsync<MultiPlace?>(sql, new { m, mp });
    }

    public async Task<PlacesResult> GetOpenPlacesByMpPrefix(short m, string mpPrefix, int page, int pageSize)
    {
        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? pageSize : 10;
        var offset = (safePage - 1) * safePageSize;

        var prefix = mpPrefix + "%";

        await using var conn = await dataSource.OpenConnectionAsync();

        const string countSql = """
                                    SELECT COUNT(*)::bigint
                                    FROM multi_places
                                    WHERE m = @m AND mp LIKE @prefix AND filling < 2;
                                """;

        var total = await conn.ExecuteScalarAsync<long>(countSql, new { m, prefix });
        if (total == 0) return new PlacesResult([], 0);

        const string sql = $"""
                                SELECT {SelectColumns}
                                FROM multi_places
                                WHERE m = @m AND mp LIKE @prefix AND filling < 2
                                ORDER BY length(mp) ASC, mp ASC
                                LIMIT @limit OFFSET @offset;
                            """;

        var items = (await conn.QueryAsync<MultiPlace>(sql, new
        {
            m,
            prefix,
            limit = safePageSize,
            offset
        })).ToArray();

        return new PlacesResult(items, total);
    }
}
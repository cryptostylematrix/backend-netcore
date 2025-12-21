using Dapper;
using Npgsql;

namespace Matrix.Infrastructure.Data.Repositories;

public sealed class LockReadOnlyRepository(NpgsqlDataSource dataSource) : ILockReadOnlyRepository
{
    private const string SelectColumns = """
                                                 id                  AS Id,
                                                 mp                  AS Mp,
                                                 m                   AS M,
                                                 profile_addr        AS ProfileAddr,
                                                 place_addr          AS PlaceAddr,
                                                 locked_pos          AS LockedPos,
                                                 place_profile_login AS PlaceProfileLogin,
                                                 place_number        AS PlaceNumber,
                                                 craeted_at          AS CreatedAt,
                                                 confirmed           AS Confirmed,
                                                 task_key            AS TaskKey,
                                                 task_query_id       AS TaskQueryId,
                                                 task_source_addr    AS TaskSourceAddr
                                         """;

    public async Task<LocksResult> GetLocks(short m, string profileAddr, int page, int pageSize)
    {
        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? pageSize : 10;
        var offset = (safePage - 1) * safePageSize;

        await using var conn = await dataSource.OpenConnectionAsync();

        const string countSql = """
                                    SELECT COUNT(*)::bigint
                                    FROM multi_locks2
                                    WHERE m = @m AND profile_addr = @profileAddr;
                                """;

        var totalLong = await conn.ExecuteScalarAsync<long>(countSql, new
        {
            m, profileAddr
        });

        if (totalLong <= 0)
            return new LocksResult([], 0);

        const string sql = $"""
                                SELECT {SelectColumns}
                                FROM multi_locks2
                                WHERE m = @m AND profile_addr = @profileAddr
                                ORDER BY place_number ASC
                                LIMIT @limit OFFSET @offset;
                            """;

        var items = await conn.QueryAsync<MultiLock>(sql, new
        {
            m,
            profileAddr,
            limit = safePageSize,
            offset
        });

        return new LocksResult(items, totalLong);
    }

    public async Task<MultiLock?> GetLockByPlaceAddrAndLockedPos(string placeAddr, short lockedPos, string profileAddr)
    {
        await using var conn = await dataSource.OpenConnectionAsync();

        const string sql = $"""
                                SELECT {SelectColumns}
                                FROM multi_locks2
                                WHERE place_addr = @placeAddr
                                  AND locked_pos = @lockedPos
                                  AND profile_addr = @profileAddr
                                LIMIT 1;
                            """;

        return await conn.QuerySingleOrDefaultAsync<MultiLock?>(sql, new { placeAddr, lockedPos, profileAddr });
    }
}

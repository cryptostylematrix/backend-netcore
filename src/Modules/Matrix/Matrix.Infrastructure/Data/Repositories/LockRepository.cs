using Dapper;
using Npgsql;

namespace Matrix.Infrastructure.Data.Repositories;

public sealed class LockRepository(NpgsqlDataSource dataSource) : ILockRepository
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

    public async Task<MultiLock> AddLock(MultiLock multiLock)
    {
        await using var conn = await dataSource.OpenConnectionAsync();

        const string sql = $"""
                                        INSERT INTO multi_locks2 (
                                            task_key,
                                            task_query_id,
                                            task_source_addr,
                                            confirmed,
                                            mp,
                                            m,
                                            profile_addr,
                                            place_addr,
                                            locked_pos,
                                            place_profile_login,
                                            place_number,
                                            craeted_at
                                        )
                                        VALUES (
                                            @TaskKey,
                                            @TaskQueryId,
                                            @TaskSourceAddr,
                                            @Confirmed,
                                            @Mp,
                                            @M,
                                            @ProfileAddr,
                                            @PlaceAddr,
                                            @LockedPos,
                                            @PlaceProfileLogin,
                                            @PlaceNumber,
                                            @CreatedAt
                                        )
                                        RETURNING {SelectColumns};
                            """;

        var inserted = await conn.QuerySingleOrDefaultAsync<MultiLock?>(sql, multiLock);
        return inserted ?? throw new InvalidOperationException("Failed to insert lock");
    }

    public async Task<MultiLock> UpdateLockConfirm(int id)
    {
        await using var conn = await dataSource.OpenConnectionAsync();

        const string sql = $"""
                                        UPDATE multi_locks2
                                        SET confirmed = TRUE
                                        WHERE id = @id
                                        RETURNING {SelectColumns};
                            """;

        var updated = await conn.QuerySingleOrDefaultAsync<MultiLock?>(sql, new { id });
        return updated ?? throw new InvalidOperationException($"Failed to update lock {id}");
    }

    public async Task RemoveLock(int id)
    {
        await using var conn = await dataSource.OpenConnectionAsync();

        const string sql = """
                                       DELETE FROM multi_locks2
                                       WHERE id = @id
                                       RETURNING id;
                           """;

        var deletedId = await conn.QuerySingleOrDefaultAsync<int?>(sql, new { id });
        if (deletedId is null)
            throw new InvalidOperationException("Failed to remove lock");
    }

    // If IRepository<MultiLock> requires more members, implement them here.
}

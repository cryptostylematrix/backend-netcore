namespace Matrix.Infrastructure.Data.Repositories;

public sealed class PlaceRepository : IPlaceRepository
{
    public Task IncrementFilling(int id)
    {
        throw new NotImplementedException();
    }

    public Task IncrementFilling2(int id)
    {
        throw new NotImplementedException();
    }

    public Task<MultiPlace> UpdatePlaceAddressAndConfirm(int id, string addr)
    {
        throw new NotImplementedException();
    }

    public Task<MultiPlace> AddPlace(MultiPlace multiPlace)
    {
        throw new NotImplementedException();
    }
    
    
    // public async Task IncrementFilling(int id)
    // {
    //     await using var conn = await dataSource.OpenConnectionAsync();
    //
    //     const string sql = @"UPDATE multi_places SET filling = filling + 1 WHERE id = @id;";
    //     await conn.ExecuteAsync(sql, new { id });
    // }
    //
    // public async Task IncrementFilling2(int id)
    // {
    //     await using var conn = await dataSource.OpenConnectionAsync();
    //
    //     const string sql = @"UPDATE multi_places SET filling2 = filling2 + 1 WHERE id = @id;";
    //     await conn.ExecuteAsync(sql, new { id });
    // }

    // public async Task<MultiPlace> UpdatePlaceAddressAndConfirm(int id, string addr)
    // {
    //     await using var conn = await dataSource.OpenConnectionAsync();
    //
    //     var sql = $@"
    //         UPDATE multi_places
    //         SET addr = @addr, confirmed = TRUE
    //         WHERE id = @id
    //         RETURNING {SelectColumns};
    //     ";
    //
    //     var updated = await conn.QuerySingleOrDefaultAsync<MultiPlace?>(sql, new { id, addr });
    //     return updated ?? throw new InvalidOperationException($"Failed to update place {id} with address {addr}");
    // }
    
    // public async Task<MultiPlace> AddPlace(MultiPlace multiPlace)
    // {
    //     await using var conn = await dataSource.OpenConnectionAsync();
    //
    //     // Note:
    //     // - DB column is "craeted_at" in your TS code, so we insert into craeted_at.
    //     // - "index" is a reserved-ish identifier; keep it quoted.
    //     // - filling/filling2 default to 0 like TS.
    //     var sql = $@"
    //         INSERT INTO multi_places (
    //             m, profile_addr, addr, parent_addr, parent_id, mp, pos, place_number, craeted_at,
    //             filling, filling2, clone, profile_login, ""index"",
    //             task_key, task_query_id, task_source_addr, inviter_profile_addr, confirmed
    //         )
    //         VALUES (
    //             @M, @ProfileAddr, @Addr, @ParentAddr, @ParentId, @Mp, @Pos, @PlaceNumber, @CreatedAt,
    //             0, 0, @Clone, @ProfileLogin, @Index,
    //             @TaskKey, @TaskQueryId, @TaskSourceAddr, @InviterProfileAddr, @Confirmed
    //         )
    //         RETURNING {SelectColumns};
    //     ";
    //
    //     var inserted = await conn.QuerySingleOrDefaultAsync<MultiPlace?>(sql, multiPlace);
    //     return inserted ?? throw new InvalidOperationException("Failed to insert place");
    // }
}
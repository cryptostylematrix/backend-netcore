namespace Marketing.Infrastructure.Queries;

public sealed class LockQueries(NpgsqlDataSource dataSource) : ILockQueries
{
    private const string LockSelectSql = """
        SELECT
            marketing_addr       AS "MarketingAddr",
            m                    AS "M",
            profile_addr         AS "ProfileAddr",
            place_addr           AS "PlaceAddr",
            locked_pos           AS "LockedPos",
            place_profile_login  AS "PlaceProfileLogin",
            place_number         AS "PlaceNumber",
            created_at           AS "CreatedAt"
        FROM marketing_locks
        WHERE confirmed = true
        """;

    private static (int Page, int PageSize, int Offset) NormalizePaging(int page, int pageSize)
    {
        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? pageSize : 10;
        return (safePage, safePageSize, (safePage - 1) * safePageSize);
    }

    private static Paginated<LockResponse> EmptyPage(int page) => new()
    {
        Page = page,
        TotalPages = 1,
        Items = Array.Empty<LockResponse>()
    };

    public async Task<string[]> GetAllLockMpsAsync(
        string marketingAddr,
        byte m,
        string profileAddr,
        CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = """
            SELECT mp
            FROM marketing_locks
            WHERE confirmed = true
              AND marketing_addr = @marketingAddr
              AND m = @m
              AND profile_addr = @profileAddr
            ORDER BY length(mp) ASC, mp ASC;
        """;

        var mps = await conn.QueryAsync<string>(
            new CommandDefinition(sql, new { marketingAddr, m, profileAddr }, cancellationToken: ct));

        return mps.ToArray();
    }

    public async Task<Paginated<LockResponse>> GetLocksAsync(
        string marketingAddr,
        byte m,
        string profileAddr,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var (safePage, safePageSize, offset) = NormalizePaging(page, pageSize);

        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string countSql = """
            SELECT COUNT(*)::bigint
            FROM marketing_locks
            WHERE confirmed = true
              AND marketing_addr = @marketingAddr
              AND m = @m
              AND profile_addr = @profileAddr;
        """;

        var total = await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(countSql, new { marketingAddr, m, profileAddr }, cancellationToken: ct));

        if (total <= 0)
            return EmptyPage(safePage);

        const string dataSql = LockSelectSql + """
            AND marketing_addr = @marketingAddr
            AND m = @m
            AND profile_addr = @profileAddr
            ORDER BY place_number ASC
            LIMIT @limit OFFSET @offset;
        """;

        var items = (await conn.QueryAsync<LockResponse>(
            new CommandDefinition(
                dataSql,
                new { marketingAddr, m, profileAddr, limit = safePageSize, offset },
                cancellationToken: ct)))
            .AsList();

        return new Paginated<LockResponse>
        {
            Page = safePage,
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)total / safePageSize)),
            Items = items
        };
    }
}
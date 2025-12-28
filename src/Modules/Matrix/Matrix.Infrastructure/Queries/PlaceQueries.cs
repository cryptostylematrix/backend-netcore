namespace Matrix.Infrastructure.Queries;

public sealed class PlaceQueries(NpgsqlDataSource dataSource) : IPlaceQueries
{
    private const string PlaceSelectSql = """
        SELECT
            addr           AS "Addr",
            parent_addr    AS "ParentAddr",
            place_number   AS "PlaceNumber",
            craeted_at     AS "CreatedAt",
            clone          AS "Clone",
            pos            AS "Pos",
            profile_login  AS "ProfileLogin",
            m              AS "M",
            profile_addr   AS "ProfileAddr",
            filling        AS "Filling",
            filling2       AS "Filling2",
            id             AS "Id",
            parent_id      AS "ParentId",
            mp             AS "Mp"
        FROM multi_places
        WHERE confirmed = true
        """;

    private static (int Page, int PageSize, int Offset) NormalizePaging(int page, int pageSize)
    {
        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? pageSize : 10;
        return (safePage, safePageSize, (safePage - 1) * safePageSize);
    }

    private static Paginated<PlaceResponse> EmptyPage(int page) => new()
    {
        Page = page,
        TotalPages = 1,
        Items = Array.Empty<PlaceResponse>()
    };

    public async Task<long> GetPlacesCountAsync(short m, string profileAddr, CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = """
            SELECT COUNT(*)::bigint
            FROM multi_places
            WHERE confirmed = true
              AND m = @m
              AND profile_addr = @profileAddr;
        """;

        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, new { m, profileAddr }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<PlaceResponse>> GetOpenPlacesByMpPrefixAsync(
        short m,
        string mpPrefix,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var (_, safePageSize, offset) = NormalizePaging(page, pageSize);
        var prefix = mpPrefix + "%";

        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = PlaceSelectSql + """
            AND m = @m
            AND mp LIKE @prefix
            AND filling < 2
            ORDER BY length(mp) ASC, mp ASC
            LIMIT @limit OFFSET @offset;
        """;

        var items = await conn.QueryAsync<PlaceResponse>(
            new CommandDefinition(sql, new { m, prefix, limit = safePageSize, offset }, cancellationToken: ct));

        return items.AsList();
    }

    public async Task<PlaceResponse?> GetPlaceByAddressAsync(string addr, CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = PlaceSelectSql + """
            AND addr = @addr
            LIMIT 1;
        """;

        return await conn.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(sql, new { addr }, cancellationToken: ct));
    }
    
    public async Task<IReadOnlyList<PlaceResponse>?> GetPathAsync(string rootAddr, string placeAddr, CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string loadSql = PlaceSelectSql + """
            AND addr = ANY(@addrs);
        """;

        var places = (await conn.QueryAsync<PlaceResponse>(
            new CommandDefinition(loadSql, new { addrs = new[] { rootAddr, placeAddr } }, cancellationToken: ct)))
            .AsList();

        var rootPlace = places.FirstOrDefault(x => x.Addr == rootAddr);
        var targetPlace = places.FirstOrDefault(x => x.Addr == placeAddr);

        if (rootPlace is null || targetPlace is null)
            return null;

        if (rootPlace.M != targetPlace.M)
            return null;

        var rootMp = rootPlace.Mp;
        var targetMp = targetPlace.Mp;

        var rootIsAncestor = targetMp.StartsWith(rootMp, StringComparison.Ordinal);
        var targetIsAncestor = rootMp.StartsWith(targetMp, StringComparison.Ordinal);

        if (!rootIsAncestor && !targetIsAncestor)
            return null;

        var shortMp = rootIsAncestor ? rootMp : targetMp;
        var longMp = rootIsAncestor ? targetMp : rootMp;

        var mps = new List<string>(longMp.Length - shortMp.Length + 1);
        for (var cur = longMp; ; cur = cur[..^1])
        {
            mps.Add(cur);
            if (cur == shortMp) break;
            if (cur.Length == 0) return null;
            if (cur.Length - 1 < shortMp.Length) return null;
        }

        const string pathSql = PlaceSelectSql + """
            AND m = @m
            AND mp = ANY(@mps);
        """;

        var pathRows = (await conn.QueryAsync<PlaceResponse>(
            new CommandDefinition(pathSql, new { m = rootPlace.M, mps }, cancellationToken: ct)))
            .AsList();

        if (pathRows.Count != mps.Count)
            return null;

        var byMp = pathRows.ToDictionary(x => x.Mp, StringComparer.Ordinal);

        // built long->short, so reverse to short->long
        return mps
            .AsEnumerable()
            .Reverse()
            .Select(mp => byMp[mp])
            .ToList();
    }

    public async Task<IReadOnlyList<PlaceResponse>> GetPlacesByMpPrefixAsync(
        short m,
        string mpPrefix,
        int depthLevels,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var (_, safePageSize, offset) = NormalizePaging(page, pageSize);

        var safeDepth = depthLevels >= 0 ? depthLevels : 0;
        var maxLength = mpPrefix.Length + safeDepth;
        var prefix = mpPrefix + "%";

        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = PlaceSelectSql + """
            AND m = @m
            AND mp LIKE @prefix
            AND length(mp) <= @maxLength
            ORDER BY length(mp) ASC, mp ASC
            LIMIT @limit OFFSET @offset;
        """;

        var items = await conn.QueryAsync<PlaceResponse>(
            new CommandDefinition(sql, new { m, prefix, maxLength, limit = safePageSize, offset }, cancellationToken: ct));

        return items.AsList();
    }

    public async Task<long> GetPlacesCountByMpPrefixAsync(short m, string mpPrefix, CancellationToken ct)
    {
        var prefix = mpPrefix + "%";

        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = """
            SELECT COUNT(*)::bigint
            FROM multi_places
            WHERE confirmed = true
              AND m = @m
              AND mp LIKE @prefix;
        """;

        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, new { m, prefix }, cancellationToken: ct));
    }

    public async Task<Paginated<PlaceResponse>> GetPlacesAsync(short m, string profileAddr, int page, int pageSize, CancellationToken ct)
    {
        var (safePage, safePageSize, offset) = NormalizePaging(page, pageSize);

        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string countSql = """
            SELECT COUNT(*)::bigint
            FROM multi_places
            WHERE confirmed = true
              AND m = @m
              AND profile_addr = @profileAddr;
        """;

        var total = await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(countSql, new { m, profileAddr }, cancellationToken: ct));

        if (total <= 0)
            return EmptyPage(safePage);

        const string dataSql = PlaceSelectSql + """
            AND m = @m
            AND profile_addr = @profileAddr
            ORDER BY place_number ASC
            LIMIT @limit OFFSET @offset;
        """;

        var items = (await conn.QueryAsync<PlaceResponse>(
            new CommandDefinition(dataSql, new { m, profileAddr, limit = safePageSize, offset }, cancellationToken: ct)))
            .AsList();

        return new Paginated<PlaceResponse>
        {
            Page = safePage,
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)total / safePageSize)),
            Items = items
        };
    }

    public async Task<PlaceResponse?> GetRootPlaceAsync(short m, string profileAddr, CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = PlaceSelectSql + """
            AND m = @m
            AND profile_addr = @profileAddr
            AND place_number = 1
            LIMIT 1;
        """;

        return await conn.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(sql, new { m, profileAddr }, cancellationToken: ct));
    }

    public async Task<Paginated<PlaceResponse>> SearchPlacesAsync(short m, string profileAddr, string query, int page, int pageSize, CancellationToken ct)
    {
        var (safePage, safePageSize, offset) = NormalizePaging(page, pageSize);

        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string rootSql = PlaceSelectSql + """
            AND m = @m
            AND profile_addr = @profileAddr
            AND place_number = 1
            LIMIT 1;
        """;

        var root = await conn.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(rootSql, new { m, profileAddr }, cancellationToken: ct));

        if (root is null)
            return EmptyPage(safePage);

        var mpPrefix = root.Mp + "%";
        var indexPrefix = query + "%";

        const string countSql = """
            SELECT COUNT(*)::bigint
            FROM multi_places
            WHERE confirmed = true
              AND m = @m
              AND mp LIKE @mpPrefix
              AND "index" LIKE @indexPrefix;
        """;

        var total = await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(countSql, new { m, mpPrefix, indexPrefix }, cancellationToken: ct));

        if (total <= 0)
            return EmptyPage(safePage);

        const string dataSql = PlaceSelectSql + """
            AND m = @m
            AND mp LIKE @mpPrefix
            AND "index" LIKE @indexPrefix
            ORDER BY "index" ASC
            LIMIT @limit OFFSET @offset;
        """;

        var items = (await conn.QueryAsync<PlaceResponse>(
            new CommandDefinition(dataSql, new { m, mpPrefix, indexPrefix, limit = safePageSize, offset }, cancellationToken: ct)))
            .AsList();

        return new Paginated<PlaceResponse>
        {
            Page = safePage,
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)total / safePageSize)),
            Items = items
        };
    }
}
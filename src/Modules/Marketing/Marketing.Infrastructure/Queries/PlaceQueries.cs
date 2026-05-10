namespace Marketing.Infrastructure.Queries;

public sealed class PlaceQueries(NpgsqlDataSource dataSource) : IPlaceQueries
{
    private const string PlaceSelectSql = """
        SELECT
            id              AS "Id",
            parent_id       AS "ParentId",
            marketing_addr  AS "MarketingAddr",
            addr            AS "Addr",
            parent_addr     AS "ParentAddr",
            place_number    AS "PlaceNumber",
            created_at      AS "CreatedAt",
            pos             AS "Pos",
            seq_no          AS "SeqNo",
            width           AS "Width",
            height          AS "Height",
            kind            AS "Kind",
            profile_login   AS "ProfileLogin",
            m               AS "M",
            profile_addr    AS "ProfileAddr",
            mp              AS "Mp"
        FROM marketing_places
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

    public async Task<long> GetPlacesCountAsync(
        string marketingAddr,
        byte m,
        string profileAddr,
        CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = """
            SELECT COUNT(*)::bigint
            FROM marketing_places
            WHERE confirmed = true
              AND marketing_addr = @marketingAddr
              AND m = @m
              AND profile_addr = @profileAddr;
        """;

        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, new { marketingAddr, m, profileAddr }, cancellationToken: ct));
    }

    public async Task<long> GetPlacesTotalCountAsync(
        string marketingAddr,
        string profileAddr,
        CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = """
            SELECT COUNT(*)::bigint
            FROM marketing_places
            WHERE confirmed = true
              AND marketing_addr = @marketingAddr
              AND profile_addr = @profileAddr;
        """;

        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, new { marketingAddr, profileAddr }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<PlaceResponse>> GetOpenPlacesByMpPrefixAsync(
        string marketingAddr,
        byte m,
        string mpPrefix,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var (_, safePageSize, offset) = NormalizePaging(page, pageSize);
        var prefix = mpPrefix + "%";

        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = PlaceSelectSql + """
                                                AND marketing_addr = @marketingAddr
                                                AND m = @m
                                                AND mp LIKE @prefix
                                                AND (width = 0 OR seq_no < width)
                                                ORDER BY length(mp) ASC, mp ASC
                                                LIMIT @limit OFFSET @offset;
                                            """;

        var items = await conn.QueryAsync<PlaceResponse>(
            new CommandDefinition(
                sql,
                new { marketingAddr, m, prefix, limit = safePageSize, offset },
                cancellationToken: ct));

        return items.AsList();
    }

    public async Task<PlaceResponse?> GetPlaceByAddressAsync(
        string marketingAddr,
        string placeAddr,
        CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = PlaceSelectSql + """
            AND marketing_addr = @marketingAddr
            AND addr = @placeAddr
            LIMIT 1;
        """;

        return await conn.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(sql, new { marketingAddr, placeAddr }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<PlaceResponse>?> GetPathAsync(
        string marketingAddr,
        string rootAddr,
        string placeAddr,
        CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string loadSql = PlaceSelectSql + """
            AND marketing_addr = @marketingAddr
            AND addr = ANY(@addrs);
        """;

        var places = (await conn.QueryAsync<PlaceResponse>(
            new CommandDefinition(
                loadSql,
                new { marketingAddr, addrs = new[] { rootAddr, placeAddr } },
                cancellationToken: ct)))
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

        var mps = new List<string>();

        for (var cur = longMp; ; cur = cur[..^8])
        {
            mps.Add(cur);

            if (cur == shortMp)
                break;

            if (cur.Length <= 8 || cur.Length - 8 < shortMp.Length)
                return null;
        }

        const string pathSql = PlaceSelectSql + """
            AND marketing_addr = @marketingAddr
            AND m = @m
            AND mp = ANY(@mps);
        """;

        var pathRows = (await conn.QueryAsync<PlaceResponse>(
            new CommandDefinition(
                pathSql,
                new { marketingAddr, m = rootPlace.M, mps },
                cancellationToken: ct)))
            .AsList();

        if (pathRows.Count != mps.Count)
            return null;

        var byMp = pathRows.ToDictionary(x => x.Mp, StringComparer.Ordinal);

        return mps
            .AsEnumerable()
            .Reverse()
            .Select(mp => byMp[mp])
            .ToList();
    }

    public async Task<IReadOnlyList<PlaceResponse>> GetPlacesByMpPrefixAsync(
        string marketingAddr,
        byte m,
        string mpPrefix,
        int depthLevels,
        uint fromPos,
        uint toPos,
        CancellationToken ct)
    {
        if (fromPos > toPos)
            return [];

        var safeDepth = Math.Max(depthLevels, 0);
        var maxLength = mpPrefix.Length + safeDepth * 8;
        var prefix = mpPrefix + "%";

        var fromHex = fromPos.ToString("X8");
        var toHex = toPos.ToString("X8");
        var prefixLen = mpPrefix.Length;

        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = PlaceSelectSql + """
            AND marketing_addr = @marketingAddr
            AND m = @m
            AND mp LIKE @prefix
            AND length(mp) <= @maxLength

            AND NOT EXISTS (
                SELECT 1
                FROM generate_series(@prefixLen + 1, length(mp), 8) AS s(pos)
                WHERE substring(mp from s.pos for 8) < @fromHex
                   OR substring(mp from s.pos for 8) > @toHex
            )

            ORDER BY length(mp) ASC, mp ASC;
        """;

        var items = await conn.QueryAsync<PlaceResponse>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    m,
                    prefix,
                    maxLength,
                    prefixLen,
                    fromHex,
                    toHex
                },
                cancellationToken: ct));

        return items.AsList();
    }

    public async Task<long> GetPlacesCountByMpPrefixAsync(
        string marketingAddr,
        byte m,
        string mpPrefix,
        CancellationToken ct)
    {
        var prefix = mpPrefix + "%";

        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = """
            SELECT COUNT(*)::bigint
            FROM marketing_places
            WHERE confirmed = true
              AND marketing_addr = @marketingAddr
              AND m = @m
              AND mp LIKE @prefix;
        """;

        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, new { marketingAddr, m, prefix }, cancellationToken: ct));
    }

    public async Task<Paginated<PlaceResponse>> GetPlacesAsync(
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
            FROM marketing_places
            WHERE confirmed = true
              AND marketing_addr = @marketingAddr
              AND m = @m
              AND profile_addr = @profileAddr;
        """;

        var total = await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(countSql, new { marketingAddr, m, profileAddr }, cancellationToken: ct));

        if (total <= 0)
            return EmptyPage(safePage);

        const string dataSql = PlaceSelectSql + """
            AND marketing_addr = @marketingAddr
            AND m = @m
            AND profile_addr = @profileAddr
            ORDER BY place_number ASC
            LIMIT @limit OFFSET @offset;
        """;

        var items = (await conn.QueryAsync<PlaceResponse>(
            new CommandDefinition(
                dataSql,
                new { marketingAddr, m, profileAddr, limit = safePageSize, offset },
                cancellationToken: ct)))
            .AsList();

        return new Paginated<PlaceResponse>
        {
            Page = safePage,
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)total / safePageSize)),
            Items = items
        };
    }

    public async Task<PlaceResponse?> GetRootPlaceAsync(
        string marketingAddr,
        byte m,
        string profileAddr,
        CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = PlaceSelectSql + """
            AND marketing_addr = @marketingAddr
            AND m = @m
            AND profile_addr = @profileAddr
            AND place_number = 1
            LIMIT 1;
        """;

        return await conn.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(sql, new { marketingAddr, m, profileAddr }, cancellationToken: ct));
    }

    public async Task<Paginated<PlaceResponse>> SearchPlacesAsync(
        string marketingAddr,
        byte m,
        string profileAddr,
        string query,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var (safePage, safePageSize, offset) = NormalizePaging(page, pageSize);

        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string rootSql = PlaceSelectSql + """
            AND marketing_addr = @marketingAddr
            AND m = @m
            AND profile_addr = @profileAddr
            AND place_number = 1
            LIMIT 1;
        """;

        var root = await conn.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(rootSql, new { marketingAddr, m, profileAddr }, cancellationToken: ct));

        if (root is null)
            return EmptyPage(safePage);

        var mpPrefix = root.Mp + "%";
        var indexPrefix = query + "%";

        const string countSql = """
            SELECT COUNT(*)::bigint
            FROM marketing_places
            WHERE confirmed = true
              AND marketing_addr = @marketingAddr
              AND m = @m
              AND mp LIKE @mpPrefix
              AND "index" LIKE @indexPrefix;
        """;

        var total = await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(
                countSql,
                new { marketingAddr, m, mpPrefix, indexPrefix },
                cancellationToken: ct));

        if (total <= 0)
            return EmptyPage(safePage);

        const string dataSql = PlaceSelectSql + """
            AND marketing_addr = @marketingAddr
            AND m = @m
            AND mp LIKE @mpPrefix
            AND "index" LIKE @indexPrefix
            ORDER BY "index" ASC
            LIMIT @limit OFFSET @offset;
        """;

        var items = (await conn.QueryAsync<PlaceResponse>(
            new CommandDefinition(
                dataSql,
                new { marketingAddr, m, mpPrefix, indexPrefix, limit = safePageSize, offset },
                cancellationToken: ct)))
            .AsList();

        return new Paginated<PlaceResponse>
        {
            Page = safePage,
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)total / safePageSize)),
            Items = items
        };
    }
    
    public async Task<PlaceResponse?> GetPlaceByTaskKeyAsync(
        string marketingAddr,
        uint taskKey,
        CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = PlaceSelectSql + """
                                                AND marketing_addr = @marketingAddr
                                                AND task_key = @taskKey
                                                LIMIT 1;
                                            """;

        return await conn.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(
                sql,
                new { marketingAddr, taskKey },
                cancellationToken: ct));
    }
    
    public async Task<uint> GetMaxPlaceNumberAsync(
        string marketingAddr,
        byte m,
        string profileAddr,
        CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);

        const string sql = """
                               SELECT COALESCE(MAX(place_number), 0)
                               FROM marketing_places
                               WHERE marketing_addr = @marketingAddr
                                 AND m = @m
                                 AND profile_addr = @profileAddr;
                           """;

        var value = await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    m,
                    profileAddr
                },
                cancellationToken: ct));

        return (uint)value;
    }
}
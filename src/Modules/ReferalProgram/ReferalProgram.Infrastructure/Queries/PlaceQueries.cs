using Dapper;
using Common.Dto;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Dto;

namespace ReferalProgram.Infrastructure.Queries;

public sealed class PlaceQueries(
    [FromKeyedServices("Programs")] NpgsqlDataSource dataSource)
    : IPlaceQueries, IPositionCandidateQueries
{
    private const string PlaceSelectSql = """
        SELECT
            id                    AS "Id",
            parent_id             AS "ParentId",
            mp                    AS "Mp",
            pos_group             AS "PosGroup",
            marketing_addr        AS "MarketingAddr",
            structure_number      AS "StructNumber",
            profile_addr          AS "ProfileAddr",
            place_number          AS "PlaceNumber",
            profile_login         AS "ProfileLogin",
            "index"               AS "Index",
            parent_profile_addr   AS "ParentProfileAddr",
            parent_profile_login  AS "ParentProfileLogin",
            parent_place_number   AS "ParentPlaceNumber",
            created_at            AS "CreatedAt",
            activated_at          AS "ActivatedAt",
            is_active             AS "IsActive",
            kind                  AS "Kind",
            pos                   AS "Pos",
            filling               AS "Filling",
            deep                  AS "Deep",
            personal_volume       AS "PersonalVolume",
            group_volume          AS "GroupVolume"
        FROM public.places
        """;

    public async Task<PlaceResponse?> GetFirstPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken)
    {
        const string sql = PlaceSelectSql + "\n" + """
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND profile_addr IS NOT DISTINCT FROM @profileAddr
            ORDER BY place_number ASC, id ASC
            LIMIT 1;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber,
                    profileAddr
                },
                cancellationToken: cancellationToken));
    }

    public async Task<PlaceResponse?> GetLastPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken)
    {
        profileAddr = string.IsNullOrWhiteSpace(profileAddr)
            ? null
            : profileAddr;

        const string sql = PlaceSelectSql + "\n" + """
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND profile_addr IS NOT DISTINCT FROM @profileAddr
            ORDER BY place_number DESC, id DESC
            LIMIT 1;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber,
                    profileAddr
                },
                cancellationToken: cancellationToken));
    }

    public async Task<Paginated<PlaceWithMatrixResponse>> GetPlacesAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        long matrixSize,
        bool isMatrixStructure,
        bool onlyNotClosed,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? pageSize : 20;
        var offset = (safePage - 1) * safePageSize;
        var parameters = new
        {
            marketingAddr,
            structureNumber = (short)structureNumber,
            profileAddr,
            matrixSize,
            isMatrixStructure,
            onlyNotClosed,
            limit = safePageSize,
            offset
        };

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        const string countSql = """
            SELECT COUNT(*)::bigint
            FROM public.places
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND profile_addr = @profileAddr
              AND
              (
                  NOT @onlyNotClosed
                  OR NOT @isMatrixStructure
                  OR matrix_filling < @matrixSize
              );
            """;

        var total = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                countSql,
                parameters,
                cancellationToken: cancellationToken));

        if (total == 0)
        {
            return new Paginated<PlaceWithMatrixResponse>
            {
                Items = Array.Empty<PlaceWithMatrixResponse>(),
                Page = safePage,
                TotalPages = 1
            };
        }

        const string dataSql = """
            SELECT
                id                    AS "Id",
                parent_id             AS "ParentId",
                mp                    AS "Mp",
                pos_group             AS "PosGroup",
                marketing_addr        AS "MarketingAddr",
                structure_number      AS "StructNumber",
                profile_addr          AS "ProfileAddr",
                place_number          AS "PlaceNumber",
                profile_login         AS "ProfileLogin",
                "index"               AS "Index",
                parent_profile_addr   AS "ParentProfileAddr",
                parent_profile_login  AS "ParentProfileLogin",
                parent_place_number   AS "ParentPlaceNumber",
                created_at            AS "CreatedAt",
                activated_at          AS "ActivatedAt",
                is_active             AS "IsActive",
                kind                  AS "Kind",
                pos                   AS "Pos",
                filling               AS "Filling",
                deep                  AS "Deep",
                personal_volume       AS "PersonalVolume",
                group_volume          AS "GroupVolume",
                @matrixSize            AS "MatrixSize",
                CASE
                    WHEN @isMatrixStructure THEN matrix_filling
                    ELSE 1
                END                    AS "MatrixFilling"
            FROM public.places
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND profile_addr = @profileAddr
              AND
              (
                  NOT @onlyNotClosed
                  OR NOT @isMatrixStructure
                  OR matrix_filling < @matrixSize
              )
            ORDER BY place_number ASC, id ASC
            LIMIT @limit OFFSET @offset;
            """;

        var items = (await connection.QueryAsync<PlaceWithMatrixResponse>(
            new CommandDefinition(
                dataSql,
                parameters,
                cancellationToken: cancellationToken)))
            .AsList();

        return new Paginated<PlaceWithMatrixResponse>
        {
            Items = items,
            Page = safePage,
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)total / safePageSize))
        };
    }

    public async Task<long> GetPlacesCountAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken)
    {
        var sql = """
            SELECT COUNT(*)::bigint
            FROM public.places
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND profile_addr IS NOT DISTINCT FROM @profileAddr;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber,
                    profileAddr
                },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> HasProfilePlacesInStructuresAsync(
        string marketingAddr,
        string profileAddr,
        IReadOnlyCollection<byte> structureNumbers,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM public.places AS p
                WHERE p.marketing_addr = @marketingAddr
                  AND p.profile_addr = @profileAddr
                  AND p.structure_number = ANY(@structureNumbers)
            );
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    profileAddr,
                    structureNumbers = structureNumbers
                        .Select(number => (short)number)
                        .ToArray()
                },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyDictionary<string, PlaceTreeCounts>> GetTreeCountsByMpAsync(
        string marketingAddr,
        byte structureNumber,
        IReadOnlyCollection<string> mpPrefixes,
        CancellationToken cancellationToken)
    {
        if (mpPrefixes.Count == 0)
            return new Dictionary<string, PlaceTreeCounts>(StringComparer.Ordinal);

        const string sql = """
            WITH RECURSIVE roots AS
            (
                SELECT place.id,
                       place.mp,
                       place.matrix_filling
                FROM unnest(@mpPrefixes::text[]) AS requested(mp)
                JOIN public.places place
                  ON place.marketing_addr = @marketingAddr
                 AND place.structure_number = @structureNumber
                 AND place.mp = requested.mp
            ),
            descendants AS
            (
                SELECT roots.id AS root_id,
                       roots.id AS descendant_id
                FROM roots

                UNION ALL

                SELECT descendants.root_id,
                       child.id
                FROM descendants
                JOIN public.places child
                  ON child.parent_id = descendants.descendant_id
                 AND child.marketing_addr = @marketingAddr
                 AND child.structure_number = @structureNumber
            )
            SELECT
                roots.mp AS "Mp",
                roots.matrix_filling AS "MatrixFilling",
                GREATEST(COUNT(descendants.descendant_id) - 1, 0)::bigint
                    AS "DescendantsCount"
            FROM roots
            LEFT JOIN descendants ON descendants.root_id = roots.id
            GROUP BY roots.id, roots.mp, roots.matrix_filling;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<TreeCountRow>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber,
                    mpPrefixes = mpPrefixes.Distinct(StringComparer.Ordinal).ToArray()
                },
                cancellationToken: cancellationToken));

        return rows.ToDictionary(
            row => row.Mp,
            row => new PlaceTreeCounts(row.MatrixFilling, row.DescendantsCount),
            StringComparer.Ordinal);
    }

    private sealed record TreeCountRow(
        string Mp,
        long MatrixFilling,
        long DescendantsCount);

    public async Task<IReadOnlyDictionary<byte, long>> GetPlaceCountsByPosGroupAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                pos_group AS "PosGroup",
                COUNT(*)::bigint AS "PlaceCount"
            FROM public.places
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
            GROUP BY pos_group;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PosGroupCount>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber
                },
                cancellationToken: cancellationToken));

        return rows.ToDictionary(
            row => checked((byte)row.PosGroup),
            row => row.PlaceCount);
    }

    public async Task<IReadOnlyList<PlaceResponse>> GetUnfilledPlacesInDepthWindowAsync(
        string marketingAddr,
        byte structureNumber,
        string rootMp,
        byte width,
        byte depthSpread,
        IReadOnlyCollection<string> lockMps,
        CancellationToken cancellationToken)
    {
        var sql = """
            WITH candidates AS
            (
                SELECT *
                FROM public.places
                WHERE marketing_addr = @marketingAddr
                  AND structure_number = @structureNumber
                  AND mp LIKE @mpPrefix
                  AND is_active = true
                  AND kind <> 2
                  AND filling < @width
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM unnest(@lockMps) AS locks(lock_mp)
                      WHERE lower(mp || lpad(to_hex(filling + 1), 8, '0'))
                          LIKE lower(lock_mp) || '%'
                  )
            ),
            min_depth AS
            (
                SELECT MIN(deep) AS value
                FROM candidates
            )
            SELECT
                id                    AS "Id",
                parent_id             AS "ParentId",
                mp                    AS "Mp",
                pos_group             AS "PosGroup",
                marketing_addr        AS "MarketingAddr",
                structure_number      AS "StructNumber",
                profile_addr          AS "ProfileAddr",
                place_number          AS "PlaceNumber",
                profile_login         AS "ProfileLogin",
                "index"               AS "Index",
                parent_profile_addr   AS "ParentProfileAddr",
                parent_profile_login  AS "ParentProfileLogin",
                parent_place_number   AS "ParentPlaceNumber",
                created_at            AS "CreatedAt",
                activated_at          AS "ActivatedAt",
                is_active             AS "IsActive",
                kind                  AS "Kind",
                pos                   AS "Pos",
                filling               AS "Filling",
                deep                  AS "Deep",
                personal_volume       AS "PersonalVolume",
                group_volume          AS "GroupVolume"
            FROM candidates
            WHERE deep >= (SELECT value FROM min_depth)
              AND deep < (SELECT value FROM min_depth) + @depthSpread
            ORDER BY deep ASC, mp ASC, id ASC;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var places = await connection.QueryAsync<PlaceResponse>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber,
                    mpPrefix = rootMp + "%",
                    width = (long)width,
                    depthSpread = (long)depthSpread,
                    lockMps = lockMps.ToArray()
                },
                cancellationToken: cancellationToken));

        return places.AsList();
    }

    public async Task<PlaceResponse?> GetFirstActiveUnfilledPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        string rootMp,
        byte width,
        bool profiledPlacesPrioritized,
        byte depthSpread,
        IReadOnlyCollection<string> lockMps,
        CancellationToken cancellationToken)
    {
        var sql = """
            WITH candidates AS
            (
                SELECT *
                FROM public.places
                WHERE marketing_addr = @marketingAddr
                  AND structure_number = @structureNumber
                  AND mp LIKE @mpPrefix
                  AND is_active = true
                  AND kind <> 2
                  AND filling < @width
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM unnest(@lockMps) AS locks(lock_mp)
                      WHERE lower(mp || lpad(to_hex(filling + 1), 8, '0'))
                          LIKE lower(lock_mp) || '%'
                  )
            ),
            min_depth AS
            (
                SELECT MIN(deep) AS value
                FROM candidates
            )
            """ + PlaceSelectSql.Replace("FROM public.places", "FROM candidates") + "\n" + """
            WHERE deep >= (SELECT value FROM min_depth)
              AND deep < (SELECT value FROM min_depth) + @depthSpread
            ORDER BY
                CASE
                    WHEN @profiledPlacesPrioritized AND profile_addr IS NULL THEN 1
                    ELSE 0
                END ASC,
                filling ASC,
                activated_at ASC NULLS LAST,
                deep ASC,
                mp ASC,
                id ASC
            LIMIT 1;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber,
                    mpPrefix = rootMp + "%",
                    width = (long)width,
                    profiledPlacesPrioritized,
                    depthSpread = (long)depthSpread,
                    lockMps = lockMps.ToArray()
                },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<PlaceResponse>> GetOpenPlacesByMpPrefixAsync(
        string marketingAddr,
        byte structureNumber,
        string mpPrefix,
        byte width,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? pageSize : 50;
        var offset = (safePage - 1) * safePageSize;

        const string sql = PlaceSelectSql + "\n" + """
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND mp LIKE @mpPrefix
              AND is_active = true
              AND kind <> 2
              AND (@width = 0 OR filling < @width)
            ORDER BY length(mp) ASC, mp ASC, id ASC
            LIMIT @limit OFFSET @offset;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var places = await connection.QueryAsync<PlaceResponse>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber,
                    mpPrefix = mpPrefix + "%",
                    width = (long)width,
                    limit = safePageSize,
                    offset
                },
                cancellationToken: cancellationToken));

        return places.AsList();
    }

    public async Task<Paginated<PlaceResponse>> SearchPlacesAsync(
        string marketingAddr,
        byte structureNumber,
        string rootMp,
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? pageSize : 20;
        var offset = (safePage - 1) * safePageSize;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var searchParameters = new
        {
            marketingAddr,
            structureNumber = (short)structureNumber,
            mpPrefix = rootMp + "%",
            queryPrefix = query + "%",
            limit = safePageSize,
            offset
        };

        const string countSql = """
            SELECT COUNT(*)::bigint
            FROM public.places
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND mp LIKE @mpPrefix
              AND lower("index") LIKE lower(@queryPrefix);
            """;

        var total = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                countSql,
                searchParameters,
                cancellationToken: cancellationToken));

        if (total == 0)
        {
            return new Paginated<PlaceResponse>
            {
                Items = Array.Empty<PlaceResponse>(),
                Page = safePage,
                TotalPages = 1
            };
        }

        const string dataSql = PlaceSelectSql + "\n" + """
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND mp LIKE @mpPrefix
              AND lower("index") LIKE lower(@queryPrefix)
            ORDER BY "index" ASC, id ASC
            LIMIT @limit OFFSET @offset;
            """;

        var items = (await connection.QueryAsync<PlaceResponse>(
            new CommandDefinition(
                dataSql,
                searchParameters,
                cancellationToken: cancellationToken)))
            .AsList();

        return new Paginated<PlaceResponse>
        {
            Items = items,
            Page = safePage,
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)total / safePageSize))
        };
    }

    public async Task<PlaceResponse?> GetPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        uint placeNumber,
        CancellationToken cancellationToken)
    {
        profileAddr = string.IsNullOrWhiteSpace(profileAddr)
            ? null
            : profileAddr;

        const string sql = PlaceSelectSql + "\n" + """
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND profile_addr IS NOT DISTINCT FROM @profileAddr
              AND place_number = @placeNumber
            LIMIT 1;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber,
                    profileAddr,
                    placeNumber = (long)placeNumber
                },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<PlaceResponse>?> GetPathAsync(
        string marketingAddr,
        byte structureNumber,
        string? fromProfileAddr,
        uint fromPlaceNumber,
        string? toProfileAddr,
        uint toPlaceNumber,
        CancellationToken cancellationToken)
    {
        const string endpointsSql = PlaceSelectSql + "\n" + """
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND
              (
                  (profile_addr IS NOT DISTINCT FROM @fromProfileAddr AND place_number = @fromPlaceNumber)
                  OR
                  (profile_addr IS NOT DISTINCT FROM @toProfileAddr AND place_number = @toPlaceNumber)
              );
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var endpoints = (await connection.QueryAsync<PlaceResponse>(
            new CommandDefinition(
                endpointsSql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber,
                    fromProfileAddr,
                    fromPlaceNumber = (long)fromPlaceNumber,
                    toProfileAddr,
                    toPlaceNumber = (long)toPlaceNumber
                },
                cancellationToken: cancellationToken)))
            .AsList();

        var from = endpoints.FirstOrDefault(place =>
            place.ProfileAddr == fromProfileAddr && place.PlaceNumber == fromPlaceNumber);
        var to = endpoints.FirstOrDefault(place =>
            place.ProfileAddr == toProfileAddr && place.PlaceNumber == toPlaceNumber);

        if (from is null || to is null)
            return null;

        var fromIsAncestor = to.Mp.StartsWith(from.Mp, StringComparison.Ordinal);
        var toIsAncestor = from.Mp.StartsWith(to.Mp, StringComparison.Ordinal);

        if (!fromIsAncestor && !toIsAncestor)
            return null;

        var ancestorMp = fromIsAncestor ? from.Mp : to.Mp;
        var descendantMp = fromIsAncestor ? to.Mp : from.Mp;

        if ((descendantMp.Length - ancestorMp.Length) % 8 != 0)
            return null;

        var mps = new List<string>();
        for (var current = descendantMp; ; current = current[..^8])
        {
            mps.Add(current);

            if (current == ancestorMp)
                break;

            if (current.Length < ancestorMp.Length + 8)
                return null;
        }

        const string pathSql = PlaceSelectSql + "\n" + """
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND mp = ANY(@mps);
            """;

        var pathRows = (await connection.QueryAsync<PlaceResponse>(
            new CommandDefinition(
                pathSql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber,
                    mps = mps.ToArray()
                },
                cancellationToken: cancellationToken)))
            .AsList();

        var byMp = pathRows
            .GroupBy(place => place.Mp, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        if (mps.Any(mp => !byMp.ContainsKey(mp)))
            return null;

        return mps
            .AsEnumerable()
            .Reverse()
            .Select(mp => byMp[mp])
            .ToList();
    }

    public async Task<PlaceResponse?> GetPlaceAsync(
        int id,
        CancellationToken cancellationToken)
    {
        const string sql = PlaceSelectSql + "\n" + """
            WHERE id = @id
            LIMIT 1;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(
                sql,
                new { id },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<PlaceResponse>> GetPlacesByMpPrefixAsync(
        string marketingAddr,
        byte structureNumber,
        string mpPrefix,
        byte depthLevels,
        uint fromPos,
        uint toPos,
        CancellationToken cancellationToken)
    {
        if (fromPos > toPos)
            return [];

        var maxLength = checked(mpPrefix.Length + depthLevels * 8);
        var prefixLength = mpPrefix.Length;

        const string sql = PlaceSelectSql + "\n" + """
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND mp LIKE @prefix
              AND length(mp) <= @maxLength
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM generate_series(@prefixLength + 1, length(mp), 8) AS segment(position)
                  WHERE substring(mp from segment.position for 8) < @fromHex
                     OR substring(mp from segment.position for 8) > @toHex
              )
            ORDER BY length(mp) ASC, mp ASC, id ASC;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var places = await connection.QueryAsync<PlaceResponse>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber,
                    prefix = mpPrefix + "%",
                    maxLength,
                    prefixLength,
                    fromHex = fromPos.ToString("X8"),
                    toHex = toPos.ToString("X8")
                },
                cancellationToken: cancellationToken));

        return places.AsList();
    }

    public async Task<Paginated<PlaceResponse>> GetChildrenAsync(
        string marketingAddr,
        byte structureNumber,
        string parentProfileAddr,
        uint parentPlaceNumber,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? pageSize : 20;
        var offset = (safePage - 1) * safePageSize;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        const string countSql = """
            SELECT COUNT(*)::bigint
            FROM public.places
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND parent_profile_addr = @parentProfileAddr
              AND parent_place_number = @parentPlaceNumber;
            """;

        var parameters = new
        {
            marketingAddr,
            structureNumber = (short)structureNumber,
            parentProfileAddr,
            parentPlaceNumber = (long)parentPlaceNumber,
            limit = safePageSize,
            offset
        };

        var total = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                countSql,
                parameters,
                cancellationToken: cancellationToken));

        if (total == 0)
        {
            return new Paginated<PlaceResponse>
            {
                Items = Array.Empty<PlaceResponse>(),
                Page = safePage,
                TotalPages = 1
            };
        }

        const string dataSql = PlaceSelectSql + "\n" + """
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND parent_profile_addr = @parentProfileAddr
              AND parent_place_number = @parentPlaceNumber
            ORDER BY pos ASC, id ASC
            LIMIT @limit OFFSET @offset;
            """;

        var items = (await connection.QueryAsync<PlaceResponse>(
            new CommandDefinition(
                dataSql,
                parameters,
                cancellationToken: cancellationToken)))
            .AsList();

        return new Paginated<PlaceResponse>
        {
            Items = items,
            Page = safePage,
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)total / safePageSize))
        };
    }

    public async Task<PlaceResponse?> GetRootPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken)
    {
        const string sql = PlaceSelectSql + "\n" + """
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND place_number = 1
              AND parent_id IS NULL
            ORDER BY id ASC
            LIMIT 1;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(
                sql,
                new
                {
                    marketingAddr,
                    structureNumber = (short)structureNumber
                },
                cancellationToken: cancellationToken));
    }

    private sealed class PosGroupCount
    {
        public short PosGroup { get; init; }
        public long PlaceCount { get; init; }
    }
}

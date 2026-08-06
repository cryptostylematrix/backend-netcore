using Dapper;
using Common.Dto;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Dto;

namespace ReferalProgram.Infrastructure.Queries;

public sealed class PlaceQueries(
    [FromKeyedServices("Programs")] NpgsqlDataSource dataSource) : IPlaceQueries
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

    public async Task<Paginated<PlaceResponse>> GetPlacesAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
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
            limit = safePageSize,
            offset
        };

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        const string countSql = """
            SELECT COUNT(*)::bigint
            FROM public.places
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND profile_addr = @profileAddr;
            """;

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
              AND profile_addr = @profileAddr
            ORDER BY place_number ASC, id ASC
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

    public async Task<long> GetPlacesCountAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken)
    {
        const string sql = """
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

    public async Task<IReadOnlyList<PlaceResponse>> GetUnfilledPlacesAtMinDepthAsync(
        string marketingAddr,
        byte structureNumber,
        string rootMp,
        byte width,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH candidates AS
            (
                SELECT *
                FROM public.places
                WHERE marketing_addr = @marketingAddr
                  AND structure_number = @structureNumber
                  AND mp LIKE @mpPrefix
                  AND filling < @width
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
            WHERE deep = (SELECT value FROM min_depth)
            ORDER BY mp ASC, id ASC;
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
                    width = (long)width
                },
                cancellationToken: cancellationToken));

        return places.AsList();
    }

    public async Task<PlaceResponse?> GetFirstActiveUnfilledPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        string rootMp,
        byte width,
        CancellationToken cancellationToken)
    {
        const string sql = PlaceSelectSql + "\n" + """
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND mp LIKE @mpPrefix
              AND is_active = true
              AND filling < @width
            ORDER BY deep ASC, filling ASC, activated_at ASC NULLS LAST, id ASC
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
                    width = (long)width
                },
                cancellationToken: cancellationToken));
    }

    public async Task<Paginated<PlaceResponse>> SearchPlacesAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = page > 0 ? page : 1;
        var safePageSize = pageSize > 0 ? pageSize : 20;
        var offset = (safePage - 1) * safePageSize;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        const string rootSql = """
            SELECT mp
            FROM public.places
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND profile_addr = @profileAddr
              AND place_number = 1
            LIMIT 1;
            """;

        var parameters = new
        {
            marketingAddr,
            structureNumber = (short)structureNumber,
            profileAddr
        };
        var rootMp = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(
                rootSql,
                parameters,
                cancellationToken: cancellationToken));

        if (rootMp is null)
        {
            return new Paginated<PlaceResponse>
            {
                Items = Array.Empty<PlaceResponse>(),
                Page = safePage,
                TotalPages = 1
            };
        }

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

    public async Task<PlaceResponse?> GetPlaceByTaskKeyAsync(
        string marketingAddr,
        int taskKey,
        CancellationToken cancellationToken)
    {
        const string sql = PlaceSelectSql + "\n" + """
            WHERE marketing_addr = @marketingAddr
              AND task_key = @taskKey
            LIMIT 1;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<PlaceResponse>(
            new CommandDefinition(
                sql,
                new { marketingAddr, taskKey },
                cancellationToken: cancellationToken));
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

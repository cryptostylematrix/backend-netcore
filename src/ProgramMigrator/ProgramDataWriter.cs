using Npgsql;

namespace ProgramMigrator;

internal sealed class ProgramDataWriter(
    string connectionString,
    IMigrationProgress progress)
{
    public async Task WriteAsync(
        string marketingAddr,
        ProgramMigrationData data,
        MigrationScope scope,
        CancellationToken cancellationToken)
    {
        if (data.Places.Count == 0)
            throw new InvalidOperationException("The migration contains no places.");

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ExecuteAsync(
            connection,
            transaction,
            "LOCK TABLE public.places, public.locks IN SHARE ROW EXCLUSIVE MODE;",
            cancellationToken);

        progress.Stage("Validating destination program and structures...");
        await ValidateProgramAsync(
            connection,
            transaction,
            marketingAddr,
            data,
            scope,
            cancellationToken);

        foreach (var structure in data.Places
                     .GroupBy(place => place.StructureNumber)
                     .OrderBy(group => group.Key))
        {
            progress.Stage(
                $"Writing structure {structure.Key}: {structure.Count()} places.");
            await WriteStructureAsync(
                connection,
                transaction,
                marketingAddr,
                structure.Key,
                structure.ToArray(),
                progress,
                cancellationToken);
        }

        progress.Stage($"Writing {data.Locks.Count} locks.");
        for (var index = 0; index < data.Locks.Count; index++)
        {
            var positionLock = data.Locks[index];
            await InsertLockAsync(
                connection,
                transaction,
                marketingAddr,
                positionLock,
                cancellationToken);
            progress.Report("Locks written", index + 1, data.Locks.Count);
        }

        if (data.Locks.Count == 0)
            progress.Report("Locks written", 0, 0);

        progress.Stage("Committing destination transaction...");
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task ValidateProgramAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string marketingAddr,
        ProgramMigrationData data,
        MigrationScope scope,
        CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(
                connection,
                transaction,
                """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM public.referal_program
                    WHERE marketing_addr = @marketingAddr
                );
                """,
                marketingAddr,
                cancellationToken))
        {
            throw new InvalidOperationException(
                $"Referral program {marketingAddr} does not exist.");
        }

        var structures = data.Places
            .Select(place => place.StructureNumber)
            .Distinct()
            .Order()
            .ToArray();

        if (scope == MigrationScope.Invite && structures.Any(number => number != 0))
            throw new InvalidOperationException("Invite scope can contain only structure 0.");

        if (scope == MigrationScope.Structures && structures.Any(number => number == 0))
            throw new InvalidOperationException("Structures scope cannot contain structure 0.");

        await using (var command = new NpgsqlCommand(
                         """
                         SELECT structure_number
                         FROM public.structures
                         WHERE marketing_addr = @marketingAddr;
                         """,
                         connection,
                         transaction))
        {
            command.Parameters.AddWithValue("marketingAddr", marketingAddr);

            var found = new HashSet<byte>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                found.Add(checked((byte)reader.GetInt16(0)));

            found.RemoveWhere(number => scope == MigrationScope.Invite
                ? number != 0
                : number == 0);

            var missing = structures.Where(number => !found.Contains(number)).ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Destination structures are missing: {string.Join(", ", missing)}.");
            }

            var missingImport = found.Where(number => !structures.Contains(number)).ToArray();
            if (missingImport.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Source data is missing destination structures: "
                    + $"{string.Join(", ", missingImport)}.");
            }
        }

        var existingLocks = await ScalarAsync<long>(
            connection,
            transaction,
            """
            SELECT COUNT(*)::bigint
            FROM public.locks
            WHERE marketing_addr = @marketingAddr
              AND structure_number = ANY(@structureNumbers);
            """,
            [
                new NpgsqlParameter("marketingAddr", marketingAddr),
                new NpgsqlParameter(
                    "structureNumbers",
                    structures.Select(number => (short)number).ToArray())
            ],
            cancellationToken);

        if (existingLocks != 0)
        {
            throw new InvalidOperationException(
                $"Imported structures already contain {existingLocks} destination locks.");
        }
    }

    private static async Task WriteStructureAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string marketingAddr,
        byte structureNumber,
        IReadOnlyList<PlaceMigrationNode> nodes,
        IMigrationProgress progress,
        CancellationToken cancellationToken)
    {
        var roots = nodes.Where(node => node.ParentSourceKey is null).ToArray();
        if (roots.Length != 1)
        {
            throw new InvalidOperationException(
                $"Structure {structureNumber} must contain exactly one imported root.");
        }

        var existingCount = await ScalarAsync<long>(
            connection,
            transaction,
            """
            SELECT COUNT(*)::bigint
            FROM public.places
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber;
            """,
            [
                new NpgsqlParameter("marketingAddr", marketingAddr),
                new NpgsqlParameter("structureNumber", (short)structureNumber)
            ],
            cancellationToken);

        if (existingCount != 1)
        {
            throw new InvalidOperationException(
                $"Structure {structureNumber} must contain only its initial top place; "
                + $"found {existingCount} places.");
        }

        var rootId = await ScalarAsync<int?>(
            connection,
            transaction,
            """
            SELECT id
            FROM public.places
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber
              AND parent_id IS NULL
              AND pos = 0;
            """,
            [
                new NpgsqlParameter("marketingAddr", marketingAddr),
                new NpgsqlParameter("structureNumber", (short)structureNumber)
            ],
            cancellationToken);

        if (rootId is null)
        {
            throw new InvalidOperationException(
                $"The initial top place for structure {structureNumber} was not found.");
        }

        var root = roots[0];
        await UpdateRootAsync(
            connection,
            transaction,
            rootId.Value,
            root,
            cancellationToken);
        progress.Report($"Structure {structureNumber} written", 1, nodes.Count);

        var nodeBySourceKey = nodes.ToDictionary(node => node.SourceKey, StringComparer.Ordinal);
        var placeIds = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [root.SourceKey] = rootId.Value
        };

        var childNodes = nodes
            .Where(node => node.ParentSourceKey is not null)
            .OrderBy(node => node.Deep)
            .ThenBy(node => node.Mp, StringComparer.Ordinal)
            .ToArray();
        for (var index = 0; index < childNodes.Length; index++)
        {
            var node = childNodes[index];
            var parentSourceKey = node.ParentSourceKey!;
            if (!placeIds.TryGetValue(parentSourceKey, out var parentId)
                || !nodeBySourceKey.TryGetValue(parentSourceKey, out var parent))
            {
                throw new InvalidOperationException(
                    $"Imported parent {parentSourceKey} was not found for {node.SourceKey}.");
            }

            var placeId = await InsertPlaceAsync(
                connection,
                transaction,
                marketingAddr,
                parentId,
                parent,
                node,
                cancellationToken);
            placeIds.Add(node.SourceKey, placeId);
            progress.Report(
                $"Structure {structureNumber} written",
                index + 2,
                nodes.Count);
        }

        if (placeIds.Count != nodes.Count)
            throw new InvalidOperationException($"Not all places in structure {structureNumber} were imported.");

        await RecalculateMatrixFillingAsync(
            connection,
            transaction,
            marketingAddr,
            structureNumber,
            cancellationToken);
    }

    private static async Task UpdateRootAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int rootId,
        PlaceMigrationNode root,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE public.places
            SET profile_addr = @profileAddr,
                profile_login = @profileLogin,
                place_number = @placeNumber,
                "index" = @index,
                created_at = @createdAt,
                activated_at = @createdAt,
                is_active = true,
                filling = @filling,
                deep = 1,
                mp = '00000000',
                pos_group = 0,
                kind = @kind,
                pos = 0,
                parent_id = NULL,
                parent_profile_addr = NULL,
                parent_profile_login = NULL,
                parent_place_number = NULL,
                matrix_filling = 1
            WHERE id = @rootId;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        AddPlaceParameters(command, root);
        command.Parameters.AddWithValue("rootId", rootId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new InvalidOperationException("The initial top place could not be updated.");
    }

    private static async Task<int> InsertPlaceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string marketingAddr,
        int parentId,
        PlaceMigrationNode parent,
        PlaceMigrationNode node,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO public.places
            (
                parent_id, mp, pos_group, marketing_addr, structure_number,
                profile_addr, place_number, profile_login, "index",
                parent_profile_addr, parent_profile_login, parent_place_number,
                created_at, activated_at, is_active, kind, pos, filling, deep,
                matrix_filling
            )
            VALUES
            (
                @parentId, @mp, 0, @marketingAddr, @structureNumber,
                @profileAddr, @placeNumber, @profileLogin, @index,
                @parentProfileAddr, @parentProfileLogin, @parentPlaceNumber,
                @createdAt, @createdAt, true, @kind, @pos, @filling, @deep,
                1
            )
            RETURNING id;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        AddPlaceParameters(command, node);
        command.Parameters.AddWithValue("parentId", parentId);
        command.Parameters.AddWithValue("marketingAddr", marketingAddr);
        command.Parameters.AddWithValue("parentProfileAddr", parent.ProfileAddr);
        command.Parameters.AddWithValue("parentProfileLogin", parent.ProfileLogin);
        command.Parameters.AddWithValue("parentPlaceNumber", checked((long)parent.PlaceNumber));

        return (int)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Place {node.SourceKey} was not inserted."));
    }

    private static void AddPlaceParameters(NpgsqlCommand command, PlaceMigrationNode node)
    {
        command.Parameters.AddWithValue("mp", node.Mp);
        command.Parameters.AddWithValue("structureNumber", (short)node.StructureNumber);
        command.Parameters.AddWithValue("profileAddr", node.ProfileAddr);
        command.Parameters.AddWithValue("placeNumber", checked((long)node.PlaceNumber));
        command.Parameters.AddWithValue("profileLogin", node.ProfileLogin);
        command.Parameters.AddWithValue("index", node.ProfileLogin + node.PlaceNumber);
        command.Parameters.AddWithValue("createdAt", node.CreatedAt);
        command.Parameters.AddWithValue("kind", (short)node.Kind);
        command.Parameters.AddWithValue("pos", checked((long)node.Pos));
        command.Parameters.AddWithValue("filling", checked((long)node.Filling));
        command.Parameters.AddWithValue("deep", checked((long)node.Deep));
    }

    private static async Task InsertLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string marketingAddr,
        LockMigrationNode positionLock,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO public.locks
            (
                task_key, task_query_id, task_source_addr, marketing_addr,
                structure_number, place_profile_addr, place_number,
                place_profile_login, profile_addr, locked_pos, mp, created_at
            )
            VALUES
            (
                0, 0, NULL, @marketingAddr,
                @structureNumber, @placeProfileAddr, @placeNumber,
                @placeProfileLogin, @profileAddr, @lockedPos, @mp, @createdAt
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("marketingAddr", marketingAddr);
        command.Parameters.AddWithValue("structureNumber", (short)positionLock.StructureNumber);
        command.Parameters.AddWithValue("placeProfileAddr", positionLock.PlaceProfileAddr);
        command.Parameters.AddWithValue("placeNumber", checked((long)positionLock.PlaceNumber));
        command.Parameters.AddWithValue("placeProfileLogin", positionLock.PlaceProfileLogin);
        command.Parameters.AddWithValue("profileAddr", positionLock.ProfileAddr);
        command.Parameters.AddWithValue("lockedPos", checked((long)positionLock.LockedPos));
        command.Parameters.AddWithValue("mp", positionLock.Mp);
        command.Parameters.AddWithValue("createdAt", positionLock.CreatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task RecalculateMatrixFillingAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH RECURSIVE structure_config AS
            (
                SELECT width, height
                FROM public.structures
                WHERE marketing_addr = @marketingAddr
                  AND structure_number = @structureNumber
            ),
            ancestors AS
            (
                SELECT place.id AS descendant_id,
                       place.id AS ancestor_id,
                       place.parent_id,
                       0 AS distance
                FROM public.places place
                WHERE place.marketing_addr = @marketingAddr
                  AND place.structure_number = @structureNumber

                UNION ALL

                SELECT ancestors.descendant_id,
                       parent.id,
                       parent.parent_id,
                       ancestors.distance + 1
                FROM ancestors
                CROSS JOIN structure_config
                JOIN public.places parent
                  ON parent.id = ancestors.parent_id
                 AND parent.marketing_addr = @marketingAddr
                 AND parent.structure_number = @structureNumber
                WHERE ancestors.distance < structure_config.height
            ),
            calculated AS
            (
                SELECT place.id,
                       CASE
                           WHEN structure_config.width > 0
                            AND structure_config.height > 0
                               THEN COUNT(ancestors.descendant_id)::bigint
                           ELSE 1::bigint
                       END AS expected
                FROM public.places place
                CROSS JOIN structure_config
                LEFT JOIN ancestors ON ancestors.ancestor_id = place.id
                WHERE place.marketing_addr = @marketingAddr
                  AND place.structure_number = @structureNumber
                GROUP BY place.id, structure_config.width, structure_config.height
            )
            UPDATE public.places place
            SET matrix_filling = calculated.expected
            FROM calculated
            WHERE place.id = calculated.id;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("marketingAddr", marketingAddr);
        command.Parameters.AddWithValue("structureNumber", (short)structureNumber);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> ExistsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        string marketingAddr,
        CancellationToken cancellationToken) =>
        await ScalarAsync<bool>(
            connection,
            transaction,
            sql,
            [new NpgsqlParameter("marketingAddr", marketingAddr)],
            cancellationToken);

    private static async Task<T> ScalarAsync<T>(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        IReadOnlyCollection<NpgsqlParameter> parameters,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var parameter in parameters)
            command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? default! : (T)result;
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

using System.Globalization;
using Npgsql;

namespace ProgramMigrator;

internal sealed class LegacyStructureReader(
    string connectionString,
    IMigrationProgress progress)
{
    public async Task<ProgramMigrationData> LoadAsync(
        LegacyProgramType programType,
        string sourceMarketingAddr,
        CancellationToken cancellationToken)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var sourcePlaces = await LoadPlacesAsync(
            connection,
            programType,
            sourceMarketingAddr,
            cancellationToken);
        progress.Stage($"Loaded {sourcePlaces.Count} confirmed legacy places.");
        var places = BuildPlaces(sourcePlaces, programType, progress);
        var sourceLocks = await LoadLocksAsync(
            connection,
            programType,
            sourceMarketingAddr,
            cancellationToken);
        progress.Stage($"Loaded {sourceLocks.Count} confirmed legacy locks.");
        var locks = BuildLocks(sourceLocks, places, programType, progress);

        return new ProgramMigrationData(places, locks);
    }

    private static async Task<List<LegacyPlaceRow>> LoadPlacesAsync(
        NpgsqlConnection connection,
        LegacyProgramType programType,
        string sourceMarketingAddr,
        CancellationToken cancellationToken)
    {
        var sql = programType == LegacyProgramType.Multi
            ? """
              SELECT id,
                     parent_id,
                     m,
                     addr,
                     pos,
                     place_number,
                     craeted_at AS created_at,
                     clone AS kind,
                     profile_addr,
                     profile_login
              FROM public.multi_places
              WHERE confirmed = true
              ORDER BY m, id;
              """
            : """
              SELECT id,
                     parent_id,
                     m,
                     addr,
                     pos,
                     place_number,
                     created_at,
                     kind,
                     profile_addr,
                     profile_login
              FROM public.marketing_places
              WHERE confirmed = true
                AND marketing_addr = @sourceMarketingAddr
              ORDER BY m, id;
              """;

        await using var command = new NpgsqlCommand(sql, connection);
        if (programType == LegacyProgramType.Neo)
            command.Parameters.AddWithValue("sourceMarketingAddr", sourceMarketingAddr);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<LegacyPlaceRow>();

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new LegacyPlaceRow(
                Number<int>(reader, "id"),
                NullableNumber<int>(reader, "parent_id"),
                Number<byte>(reader, "m"),
                RequiredString(reader, "addr"),
                Number<uint>(reader, "pos"),
                Number<uint>(reader, "place_number"),
                Number<long>(reader, "created_at"),
                Number<byte>(reader, "kind"),
                RequiredString(reader, "profile_addr"),
                RequiredString(reader, "profile_login")));
        }

        return rows;
    }

    private static async Task<List<LegacyLockRow>> LoadLocksAsync(
        NpgsqlConnection connection,
        LegacyProgramType programType,
        string sourceMarketingAddr,
        CancellationToken cancellationToken)
    {
        var sql = programType == LegacyProgramType.Multi
            ? """
              SELECT m,
                     profile_addr,
                     place_addr,
                     place_profile_login,
                     place_number,
                     locked_pos,
                     craeted_at AS created_at
              FROM public.multi_locks2
              WHERE confirmed = true
              ORDER BY m, id;
              """
            : """
              SELECT m,
                     profile_addr,
                     place_addr,
                     place_profile_login,
                     place_number,
                     locked_pos,
                     created_at
              FROM public.marketing_locks
              WHERE confirmed = true
                AND marketing_addr = @sourceMarketingAddr
              ORDER BY m, id;
              """;

        await using var command = new NpgsqlCommand(sql, connection);
        if (programType == LegacyProgramType.Neo)
            command.Parameters.AddWithValue("sourceMarketingAddr", sourceMarketingAddr);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<LegacyLockRow>();

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new LegacyLockRow(
                Number<byte>(reader, "m"),
                RequiredString(reader, "profile_addr"),
                RequiredString(reader, "place_addr"),
                RequiredString(reader, "place_profile_login"),
                Number<uint>(reader, "place_number"),
                Number<uint>(reader, "locked_pos"),
                Number<long>(reader, "created_at")));
        }

        return rows;
    }

    private static IReadOnlyList<PlaceMigrationNode> BuildPlaces(
        IReadOnlyList<LegacyPlaceRow> sourcePlaces,
        LegacyProgramType programType,
        IMigrationProgress progress)
    {
        var result = new List<PlaceMigrationNode>(sourcePlaces.Count);

        foreach (var structureGroup in sourcePlaces.GroupBy(place => place.StructureNumber))
        {
            var rows = structureGroup.ToArray();
            progress.Stage(
                $"Transforming structure {structureGroup.Key}: {rows.Length} places.");
            var roots = rows.Where(row => row.ParentId is null).ToArray();
            if (roots.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Legacy structure {structureGroup.Key} must contain exactly one root; "
                    + $"found {roots.Length}.");
            }

            if (roots[0].PlaceNumber != 1)
            {
                throw new InvalidOperationException(
                    $"Legacy structure {structureGroup.Key} root must have place number 1.");
            }

            var byId = rows.ToDictionary(row => row.Id);
            var childrenByParentId = rows
                .Where(row => row.ParentId is not null)
                .GroupBy(row => row.ParentId!.Value)
                .ToDictionary(group => group.Key, group => group.OrderBy(row => row.Id).ToArray());
            var queue = new Queue<(LegacyPlaceRow Row, PlaceMigrationNode? Parent)>();
            var visited = new HashSet<int>();
            queue.Enqueue((roots[0], null));

            while (queue.TryDequeue(out var current))
            {
                if (!visited.Add(current.Row.Id))
                    throw new InvalidOperationException($"Cycle detected at legacy place {current.Row.Id}.");

                var pos = current.Parent is null
                    ? 0
                    : programType == LegacyProgramType.Multi
                        ? checked(current.Row.Pos + 1)
                        : current.Row.Pos;

                if (current.Parent is not null && pos == 0)
                    throw new InvalidOperationException($"Legacy place {current.Row.Id} has position 0.");

                var mp = current.Parent is null
                    ? "00000000"
                    : current.Parent.Mp + pos.ToString("X8");
                var deep = current.Parent is null
                    ? 1u
                    : checked(current.Parent.Deep + 1);
                childrenByParentId.TryGetValue(current.Row.Id, out var children);
                var filling = checked((uint)(children?.Length ?? 0));
                var node = new PlaceMigrationNode(
                    SourceKey(current.Row.Id),
                    current.Parent?.SourceKey,
                    current.Row.Addr,
                    current.Row.StructureNumber,
                    current.Row.ProfileAddr,
                    current.Row.ProfileLogin,
                    current.Row.PlaceNumber,
                    current.Row.CreatedAt,
                    current.Row.Kind,
                    pos,
                    filling,
                    deep,
                    mp);
                result.Add(node);
                progress.Report(
                    $"Structure {structureGroup.Key} transformed",
                    visited.Count,
                    rows.Length);

                if (children is null)
                    continue;

                foreach (var child in children)
                {
                    if (!byId.ContainsKey(child.Id)
                        || child.StructureNumber != current.Row.StructureNumber)
                    {
                        throw new InvalidOperationException(
                            $"Legacy place {child.Id} has an invalid parent relationship.");
                    }

                    queue.Enqueue((child, node));
                }
            }

            if (visited.Count != rows.Length)
            {
                throw new InvalidOperationException(
                    $"Legacy structure {structureGroup.Key} contains disconnected places.");
            }

            var duplicateMp = result
                .Where(node => node.StructureNumber == structureGroup.Key)
                .GroupBy(node => node.Mp, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateMp is not null)
            {
                throw new InvalidOperationException(
                    $"Legacy structure {structureGroup.Key} produces duplicate MP {duplicateMp.Key}.");
            }
        }

        return result;
    }

    private static IReadOnlyList<LockMigrationNode> BuildLocks(
        IReadOnlyList<LegacyLockRow> sourceLocks,
        IReadOnlyList<PlaceMigrationNode> places,
        LegacyProgramType programType,
        IMigrationProgress progress)
    {
        var placesByIdentity = places
            .Where(place => place.SourceAddr is not null)
            .ToLookup(place => new LegacyPlaceIdentity(
                place.StructureNumber,
                place.SourceAddr!,
                place.ProfileLogin,
                place.PlaceNumber));
        var result = new List<LockMigrationNode>(sourceLocks.Count);
        var seenSourceLocks = new HashSet<LegacyLockIdentity>();
        var skippedDuplicates = 0;

        for (var index = 0; index < sourceLocks.Count; index++)
        {
            var sourceLock = sourceLocks[index];
            var sourceIdentity = new LegacyLockIdentity(
                sourceLock.StructureNumber,
                sourceLock.PlaceAddr,
                sourceLock.ProfileAddr,
                sourceLock.LockedPos);
            if (!seenSourceLocks.Add(sourceIdentity))
            {
                skippedDuplicates++;
                progress.Report("Locks transformed", index + 1, sourceLocks.Count);
                continue;
            }

            var matchingPlaces = placesByIdentity[new LegacyPlaceIdentity(
                    sourceLock.StructureNumber,
                    sourceLock.PlaceAddr,
                    sourceLock.PlaceProfileLogin,
                    sourceLock.PlaceNumber)]
                .ToArray();
            if (matchingPlaces.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Lock parent {sourceLock.PlaceAddr}, login "
                    + $"{sourceLock.PlaceProfileLogin}, place {sourceLock.PlaceNumber} "
                    + $"resolved to {matchingPlaces.Length} places in structure "
                    + $"{sourceLock.StructureNumber}; expected exactly one.");
            }

            var place = matchingPlaces[0];

            var lockedPos = programType == LegacyProgramType.Multi
                ? checked(sourceLock.LockedPos + 1)
                : sourceLock.LockedPos;
            if (lockedPos == 0)
                throw new InvalidOperationException("A migrated lock position cannot be zero.");

            result.Add(new LockMigrationNode(
                sourceLock.StructureNumber,
                place.ProfileAddr,
                place.PlaceNumber,
                place.ProfileLogin,
                sourceLock.ProfileAddr,
                lockedPos,
                place.Mp + lockedPos.ToString("X8"),
                sourceLock.CreatedAt));
            progress.Report("Locks transformed", index + 1, sourceLocks.Count);
        }

        if (sourceLocks.Count == 0)
            progress.Report("Locks transformed", 0, 0);

        if (skippedDuplicates > 0)
            progress.Stage($"Skipped {skippedDuplicates} duplicate legacy locks.");

        var duplicate = result
            .GroupBy(positionLock => new
            {
                positionLock.StructureNumber,
                positionLock.PlaceProfileAddr,
                positionLock.PlaceNumber,
                positionLock.ProfileAddr,
                positionLock.LockedPos
            })
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException("The legacy data produces duplicate locks.");

        return result;
    }

    private static string SourceKey(int id) => $"legacy:{id}";

    private static T Number<T>(NpgsqlDataReader reader, string name)
        where T : IConvertible =>
        (T)Convert.ChangeType(
            reader.GetValue(reader.GetOrdinal(name)),
            typeof(T),
            CultureInfo.InvariantCulture);

    private static T? NullableNumber<T>(NpgsqlDataReader reader, string name)
        where T : struct, IConvertible
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : Number<T>(reader, name);
    }

    private static string RequiredString(NpgsqlDataReader reader, string name)
    {
        var value = reader.GetString(reader.GetOrdinal(name));
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Legacy field {name} is empty.")
            : value;
    }

    private sealed record LegacyPlaceRow(
        int Id,
        int? ParentId,
        byte StructureNumber,
        string Addr,
        uint Pos,
        uint PlaceNumber,
        long CreatedAt,
        byte Kind,
        string ProfileAddr,
        string ProfileLogin);

    private sealed record LegacyLockRow(
        byte StructureNumber,
        string ProfileAddr,
        string PlaceAddr,
        string PlaceProfileLogin,
        uint PlaceNumber,
        uint LockedPos,
        long CreatedAt);

    private sealed record LegacyPlaceIdentity(
        byte StructureNumber,
        string PlaceAddr,
        string ProfileLogin,
        uint PlaceNumber);

    private sealed record LegacyLockIdentity(
        byte StructureNumber,
        string PlaceAddr,
        string ProfileAddr,
        uint LockedPos);
}

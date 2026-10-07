using Npgsql;
using NpgsqlTypes;

namespace LegacyPlacesXmindExporter;

public static class PlaceReader
{
    public static async Task<List<Place>> ReadAsync(string connectionString, short structure, CancellationToken cancellationToken, ExportProgress? progress = null, long? rootPlaceId = null)
    {
        progress?.Start("Подключение к базе");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction))
            await readOnly.ExecuteNonQueryAsync(cancellationToken);

        // One statement gives a consistent snapshot of the selected structure.
        // UNION over IDs bounds recursion even if the source contains a cycle.
        // No per-row path arrays: deep chains must not require quadratic memory.
        var query = rootPlaceId.HasValue ? """
            WITH RECURSIVE subtree(place_id) AS (
                SELECT place_id FROM public.places
                WHERE structure = @structure AND place_id = @root_place_id
                UNION
                SELECT child.place_id
                FROM public.places AS child
                JOIN subtree AS parent ON child.parent_id = parent.place_id
                WHERE child.structure = @structure
            )
            SELECT p.place_id, p.parent_id, p.pos, p.p_type, partner.login, p.created_at
            FROM subtree
            JOIN public.places AS p ON p.place_id = subtree.place_id
            LEFT JOIN public.partners AS partner ON partner.id = p.partner_id
            """ : """
            SELECT p.place_id, p.parent_id, p.pos, p.p_type, partner.login, p.created_at
            FROM public.places AS p
            LEFT JOIN public.partners AS partner ON partner.id = p.partner_id
            WHERE p.structure = @structure
            """;
        await using var command = new NpgsqlCommand(query, connection, transaction);
        command.Parameters.AddWithValue("structure", NpgsqlDbType.Smallint, structure);
        if (rootPlaceId is { } id)
            command.Parameters.AddWithValue("root_place_id", NpgsqlDbType.Bigint, id);
        var places = new List<Place>();
        progress?.Complete();
        progress?.Start("Чтение мест (общее количество пока неизвестно)");
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                // Keep every place in the result; never silently drop a broken partner link.
                if (reader.IsDBNull(4))
                    throw new InvalidDataException($"Место {reader.GetInt64(0)}: отсутствует связанный профиль или его login.");
                places.Add(new Place(reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetInt64(1),
                    reader.GetInt16(2), reader.GetInt16(3), reader.GetString(4), reader.GetDateTime(5)));
                progress?.Advance();
            }
        }
        await transaction.CommitAsync(cancellationToken);
        progress?.Complete();
        if (rootPlaceId.HasValue && places.Count == 0)
            throw new InvalidDataException($"Место {rootPlaceId} не найдено в структуре {structure}.");
        return places;
    }
}

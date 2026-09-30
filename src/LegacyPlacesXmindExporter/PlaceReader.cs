using Npgsql;
using NpgsqlTypes;

namespace LegacyPlacesXmindExporter;

public static class PlaceReader
{
    public static async Task<List<Place>> ReadAsync(string connectionString, short structure, CancellationToken cancellationToken, ExportProgress? progress = null)
    {
        progress?.Start("Подключение к базе");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction))
            await readOnly.ExecuteNonQueryAsync(cancellationToken);

        // One statement gives a consistent snapshot of the selected structure.
        await using var command = new NpgsqlCommand("""
            SELECT p.place_id, p.parent_id, p.pos, p.p_type, partner.login, p.created_at
            FROM public.places AS p
            LEFT JOIN public.partners AS partner ON partner.id = p.partner_id
            WHERE p.structure = @structure
            """, connection, transaction);
        command.Parameters.AddWithValue("structure", NpgsqlDbType.Smallint, structure);
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
        return places;
    }
}

using Npgsql;

namespace ProgramMatrixFillingRecalculator;

internal sealed class MatrixFillingRecalculator(string connectionString)
{
    public async Task RunAsync(
        string marketingAddr,
        bool applyChanges,
        CancellationToken cancellationToken)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await ValidateSchemaAndProgramAsync(connection, marketingAddr, cancellationToken);
        var structures = await LoadStructuresAsync(connection, marketingAddr, cancellationToken);
        if (structures.Count == 0)
            throw new InvalidOperationException("The referral program has no structures.");

        await using var transaction = applyChanges
            ? await connection.BeginTransactionAsync(cancellationToken)
            : null;

        if (transaction is not null)
        {
            await ExecuteAsync(
                connection,
                transaction,
                "LOCK TABLE public.places IN SHARE ROW EXCLUSIVE MODE;",
                cancellationToken);
        }

        long totalPlaces = 0;
        long totalMismatches = 0;
        long totalUpdated = 0;

        for (var index = 0; index < structures.Count; index++)
        {
            var structure = structures[index];
            var result = await ProcessStructureAsync(
                connection,
                transaction,
                marketingAddr,
                structure,
                applyChanges,
                cancellationToken);

            totalPlaces += result.Places;
            totalMismatches += result.Mismatches;
            totalUpdated += result.Updated;

            Console.WriteLine(
                "[{0}/{1}] Structure {2}: {3} places, {4} incorrect, {5} updated ({6}, height {7}).",
                index + 1,
                structures.Count,
                structure.Number,
                result.Places,
                result.Mismatches,
                result.Updated,
                structure.IsMatrix ? "matrix" : "non-matrix",
                structure.Height);
        }

        if (transaction is not null)
        {
            var remaining = await CountAllMismatchesAsync(
                connection,
                transaction,
                marketingAddr,
                structures,
                cancellationToken);
            if (remaining != 0)
            {
                throw new InvalidOperationException(
                    $"Verification found {remaining} incorrect matrix filling values; changes were rolled back.");
            }

            await transaction.CommitAsync(cancellationToken);
        }

        Console.WriteLine();
        Console.WriteLine("Marketing:  {0}", marketingAddr);
        Console.WriteLine("Structures: {0}", structures.Count);
        Console.WriteLine("Places:     {0}", totalPlaces);
        Console.WriteLine("Incorrect:  {0}", totalMismatches);
        Console.WriteLine("Updated:    {0}", totalUpdated);
        Console.WriteLine(applyChanges
            ? "Recalculation committed successfully."
            : "Dry run complete; no database changes were made. Run again with --apply to update them.");
    }

    private static async Task<StructureResult> ProcessStructureAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string marketingAddr,
        StructureInfo structure,
        bool applyChanges,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH RECURSIVE ancestors AS
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
                JOIN public.places parent
                  ON parent.id = ancestors.parent_id
                 AND parent.marketing_addr = @marketingAddr
                 AND parent.structure_number = @structureNumber
                WHERE ancestors.distance < @height
            ),
            calculated AS
            (
                SELECT place.id,
                       CASE
                           WHEN @isMatrix THEN COUNT(ancestors.descendant_id)::bigint
                           ELSE 1::bigint
                       END AS expected
                FROM public.places place
                LEFT JOIN ancestors ON ancestors.ancestor_id = place.id
                WHERE place.marketing_addr = @marketingAddr
                  AND place.structure_number = @structureNumber
                GROUP BY place.id
            ),
            changed AS
            (
                UPDATE public.places place
                SET matrix_filling = calculated.expected
                FROM calculated
                WHERE @applyChanges
                  AND place.id = calculated.id
                  AND place.matrix_filling IS DISTINCT FROM calculated.expected
                RETURNING place.id
            )
            SELECT COUNT(*)::bigint AS places,
                   COUNT(*) FILTER
                   (
                       WHERE place.matrix_filling IS DISTINCT FROM calculated.expected
                   )::bigint AS mismatches,
                   (SELECT COUNT(*)::bigint FROM changed) AS updated
            FROM calculated
            JOIN public.places place ON place.id = calculated.id;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("marketingAddr", marketingAddr);
        command.Parameters.AddWithValue("structureNumber", (short)structure.Number);
        command.Parameters.AddWithValue("height", (int)structure.Height);
        command.Parameters.AddWithValue("isMatrix", structure.IsMatrix);
        command.Parameters.AddWithValue("applyChanges", applyChanges);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new StructureResult(0, 0, 0);

        return new StructureResult(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2));
    }

    private static async Task<long> CountAllMismatchesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string marketingAddr,
        IReadOnlyList<StructureInfo> structures,
        CancellationToken cancellationToken)
    {
        long result = 0;
        foreach (var structure in structures)
        {
            var check = await ProcessStructureAsync(
                connection,
                transaction,
                marketingAddr,
                structure,
                applyChanges: false,
                cancellationToken);
            result += check.Mismatches;
        }

        return result;
    }

    private static async Task ValidateSchemaAndProgramAsync(
        NpgsqlConnection connection,
        string marketingAddr,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS
                   (
                       SELECT 1
                       FROM information_schema.columns
                       WHERE table_schema = 'public'
                         AND table_name = 'places'
                         AND column_name = 'matrix_filling'
                   ),
                   EXISTS
                   (
                       SELECT 1
                       FROM public.referal_program
                       WHERE marketing_addr = @marketingAddr
                   );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("marketingAddr", marketingAddr);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        if (!reader.GetBoolean(0))
        {
            throw new InvalidOperationException(
                "Column public.places.matrix_filling does not exist. Run database script 020 first.");
        }

        if (!reader.GetBoolean(1))
            throw new InvalidOperationException($"Referral program {marketingAddr} was not found.");
    }

    private static async Task<IReadOnlyList<StructureInfo>> LoadStructuresAsync(
        NpgsqlConnection connection,
        string marketingAddr,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT structure_number, width, height
            FROM public.structures
            WHERE marketing_addr = @marketingAddr
            ORDER BY structure_number;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("marketingAddr", marketingAddr);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<StructureInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new StructureInfo(
                checked((byte)reader.GetInt16(0)),
                checked((byte)reader.GetInt16(1)),
                checked((byte)reader.GetInt16(2))));
        }

        return result;
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

    private sealed record StructureInfo(byte Number, byte Width, byte Height)
    {
        public bool IsMatrix => Width > 0 && Height > 0;
    }

    private sealed record StructureResult(long Places, long Mismatches, long Updated);
}

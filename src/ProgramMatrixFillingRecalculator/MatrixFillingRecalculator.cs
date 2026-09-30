using Npgsql;
using NpgsqlTypes;

namespace ProgramMatrixFillingRecalculator;

internal sealed class MatrixFillingRecalculator(string connectionString)
{
    public async Task<bool> RunAsync(
        string? marketingAddr,
        bool applyChanges,
        CancellationToken cancellationToken)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await ValidateSchemaAsync(connection, cancellationToken);
        var marketingAddresses = await LoadMarketingAddressesAsync(
            connection,
            marketingAddr,
            cancellationToken);

        if (marketingAddresses.Count == 0)
        {
            throw new InvalidOperationException(marketingAddr is null
                ? "No Referral Programs were found."
                : $"Referral program {marketingAddr} was not found.");
        }

        var correct = true;
        Console.WriteLine("Programs selected: {0}", marketingAddresses.Count);
        for (var index = 0; index < marketingAddresses.Count; index++)
        {
            Console.WriteLine();
            Console.WriteLine(
                "=== Program {0}/{1}: {2} ===",
                index + 1,
                marketingAddresses.Count,
                marketingAddresses[index]);
            if (!applyChanges)
            {
                correct &= await CheckProgramAsync(connection, marketingAddresses[index], cancellationToken);
                continue;
            }

            await RecalculateProgramAsync(
                connection,
                marketingAddresses[index],
                applyChanges,
                cancellationToken);
        }

        return correct;
    }

    private static async Task<bool> CheckProgramAsync(
        NpgsqlConnection connection,
        string marketingAddr,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(
            System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        await ExecuteAsync(connection, transaction, "SET TRANSACTION READ ONLY;", cancellationToken);
        var structures = await LoadStructuresAsync(connection, marketingAddr, cancellationToken);
        if (structures.Count == 0)
            throw new InvalidOperationException("The referral program has no structures.");

        long fillingErrors = 0;
        long matrixErrors = 0;
        foreach (var structure in structures)
        {
            const string sql = """
                WITH RECURSIVE ancestors AS
                (
                    SELECT place.id AS descendant_id, place.id AS ancestor_id,
                           place.parent_id, 0 AS distance
                    FROM public.places place
                    WHERE place.marketing_addr = @marketingAddr
                      AND place.structure_number = @structureNumber
                    UNION ALL
                    SELECT ancestors.descendant_id, parent.id, parent.parent_id,
                           ancestors.distance + 1
                    FROM ancestors
                    JOIN public.places parent
                      ON parent.id = ancestors.parent_id
                     AND parent.marketing_addr = @marketingAddr
                     AND parent.structure_number = @structureNumber
                    WHERE ancestors.distance < @height
                ),
                matrix_counts AS
                (
                    SELECT ancestor_id, COUNT(*)::bigint AS expected
                    FROM ancestors
                    GROUP BY ancestor_id
                ),
                child_counts AS
                (
                    SELECT parent_id, COUNT(*)::bigint AS expected
                    FROM public.places
                    WHERE marketing_addr = @marketingAddr
                      AND structure_number = @structureNumber
                    GROUP BY parent_id
                )
                SELECT place.id, place.filling, COALESCE(child_counts.expected, 0),
                       place.matrix_filling,
                       CASE WHEN @isMatrix THEN matrix_counts.expected ELSE 1::bigint END
                FROM public.places place
                JOIN matrix_counts ON matrix_counts.ancestor_id = place.id
                LEFT JOIN child_counts ON child_counts.parent_id = place.id
                WHERE place.marketing_addr = @marketingAddr
                  AND place.structure_number = @structureNumber
                ORDER BY place.id;
                """;
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("marketingAddr", marketingAddr);
            command.Parameters.AddWithValue("structureNumber", (short)structure.Number);
            command.Parameters.AddWithValue("height", structure.IsMatrix ? (int)structure.Height : 0);
            command.Parameters.AddWithValue("isMatrix", structure.IsMatrix);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            long places = 0;
            long incorrectFilling = 0;
            long incorrectMatrix = 0;
            while (await reader.ReadAsync(cancellationToken))
            {
                places++;
                var expectedFilling = reader.GetInt64(2);
                var expectedMatrix = reader.GetInt64(4);
                var fillingWrong = reader.IsDBNull(1) || reader.GetInt64(1) != expectedFilling;
                var matrixWrong = reader.IsDBNull(3) || reader.GetInt64(3) != expectedMatrix;
                if (fillingWrong) incorrectFilling++;
                if (matrixWrong) incorrectMatrix++;
                if (fillingWrong || matrixWrong)
                {
                    Console.WriteLine(
                        "Structure {0}, place {1}: filling {2} (expected {3}); matrix_filling {4} (expected {5}).",
                        structure.Number, reader.GetValue(0),
                        reader.IsDBNull(1) ? "NULL" : reader.GetValue(1), expectedFilling,
                        reader.IsDBNull(3) ? "NULL" : reader.GetValue(3), expectedMatrix);
                }
            }

            fillingErrors += incorrectFilling;
            matrixErrors += incorrectMatrix;
            Console.WriteLine("Structure {0}: {1} places, filling incorrect: {2}, matrix_filling incorrect: {3}.",
                structure.Number, places, incorrectFilling, incorrectMatrix);
        }

        await transaction.CommitAsync(cancellationToken);
        Console.WriteLine("Check complete: filling incorrect: {0}; matrix_filling incorrect: {1}. No database changes were made.",
            fillingErrors, matrixErrors);
        return fillingErrors == 0 && matrixErrors == 0;
    }

    private static async Task RecalculateProgramAsync(
        NpgsqlConnection connection,
        string marketingAddr,
        bool applyChanges,
        CancellationToken cancellationToken)
    {
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

    private static async Task ValidateSchemaAsync(
        NpgsqlConnection connection,
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
                   );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        var exists = (bool)(await command.ExecuteScalarAsync(cancellationToken)
            ?? false);

        if (!exists)
        {
            throw new InvalidOperationException(
                "Column public.places.matrix_filling does not exist. Run database script 020 first.");
        }
    }

    private static async Task<IReadOnlyList<string>> LoadMarketingAddressesAsync(
        NpgsqlConnection connection,
        string? marketingAddr,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT marketing_addr
            FROM public.referal_program
            WHERE @marketingAddr IS NULL OR marketing_addr = @marketingAddr
            ORDER BY marketing_addr;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add("marketingAddr", NpgsqlDbType.Varchar).Value =
            marketingAddr is null ? DBNull.Value : marketingAddr;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(reader.GetString(0));

        return result;
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

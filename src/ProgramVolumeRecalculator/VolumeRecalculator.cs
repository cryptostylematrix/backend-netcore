using Npgsql;

namespace ProgramVolumeRecalculator;

internal sealed class VolumeRecalculator(string connectionString)
{
    public async Task RunAsync(
        RecalculationOptions options,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Marketing: {0}", options.MarketingAddr);
        Console.WriteLine("Structure: {0}", options.StructureNumber);
        Console.WriteLine("Type:      {0}", options.Type.ToString().ToLowerInvariant());

        if (options.Type == VolumeType.Group)
        {
            Console.WriteLine("Group volume recalculation is not implemented; no changes were made.");
            return;
        }

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await ValidateTargetAsync(connection, options, cancellationToken);

        var before = await InspectAsync(connection, transaction: null, options, cancellationToken);
        Console.WriteLine("Profiles:  {0}", before.Profiles);
        Console.WriteLine("Different: {0}", before.Mismatches);

        if (!options.ApplyChanges)
        {
            Console.WriteLine("Dry run complete; no database changes were made. Run again with --apply to update them.");
            return;
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await ExecuteAsync(connection, transaction,
            "LOCK TABLE public.places, public.profile_volumes IN SHARE ROW EXCLUSIVE MODE;",
            cancellationToken);
        await ApplyAsync(connection, transaction, options, cancellationToken);

        var after = await InspectAsync(connection, transaction, options, cancellationToken);
        if (after.Mismatches != 0)
            throw new InvalidOperationException(
                $"Verification found {after.Mismatches} incorrect volume values; changes were rolled back.");

        await transaction.CommitAsync(cancellationToken);
        Console.WriteLine("Updated:   {0}", before.Mismatches);
        Console.WriteLine("Recalculation committed successfully.");
    }

    private static async Task ValidateTargetAsync(
        NpgsqlConnection connection,
        RecalculationOptions options,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS
            (
                SELECT 1
                FROM public.structures
                WHERE marketing_addr = @marketingAddr
                  AND structure_number = @structureNumber
            ) AND to_regclass('public.profile_volumes') IS NOT NULL;
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("marketingAddr", options.MarketingAddr);
        command.Parameters.AddWithValue("structureNumber", (short)options.StructureNumber);
        if (!((bool?)await command.ExecuteScalarAsync(cancellationToken) ?? false))
            throw new InvalidOperationException(
                "The requested structure or public.profile_volumes table was not found. Run database script 026 first.");
    }

    private static async Task<InspectionResult> InspectAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        RecalculationOptions options,
        CancellationToken cancellationToken)
    {
        var column = Column(options.Type);
        var calculated = CalculatedSql(options.Type);
        var sql = $$"""
            WITH calculated AS
            (
                {{calculated}}
            ),
            compared AS
            (
                SELECT COALESCE(current.profile_addr, calculated.profile_addr) AS profile_addr,
                       COALESCE(current.{{column}}, 0) AS current_value,
                       COALESCE(calculated.expected, 0) AS expected
                FROM
                (
                    SELECT profile_addr, {{column}}
                    FROM public.profile_volumes
                    WHERE marketing_addr = @marketingAddr
                      AND structure_number = @structureNumber
                ) current
                FULL JOIN calculated USING (profile_addr)
            )
            SELECT COUNT(*)::bigint,
                   COUNT(*) FILTER (WHERE current_value IS DISTINCT FROM expected)::bigint
            FROM compared;
            """;
        await using var command = Command(sql, connection, transaction, options);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new InspectionResult(reader.GetInt64(0), reader.GetInt64(1));
    }

    private static async Task ApplyAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        RecalculationOptions options,
        CancellationToken cancellationToken)
    {
        var column = Column(options.Type);
        var calculated = CalculatedSql(options.Type);
        var resetSql = $$"""
            UPDATE public.profile_volumes
            SET {{column}} = 0
            WHERE marketing_addr = @marketingAddr
              AND structure_number = @structureNumber;
            """;
        await using (var reset = Command(resetSql, connection, transaction, options))
            await reset.ExecuteNonQueryAsync(cancellationToken);

        var upsertSql = $$"""
            WITH calculated AS
            (
                {{calculated}}
            )
            INSERT INTO public.profile_volumes
            (
                marketing_addr, structure_number, profile_addr,
                personal_volume, referral_volume, group_volume
            )
            SELECT @marketingAddr,
                   @structureNumber,
                   profile_addr,
                   CASE WHEN @isPersonal THEN expected ELSE 0 END,
                   CASE WHEN @isPersonal THEN 0 ELSE expected END,
                   0
            FROM calculated
            ON CONFLICT (marketing_addr, structure_number, profile_addr)
            DO UPDATE SET {{column}} = EXCLUDED.{{column}};
            """;
        await using var command = Command(upsertSql, connection, transaction, options);
        command.Parameters.AddWithValue("isPersonal", options.Type == VolumeType.Personal);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string CalculatedSql(VolumeType type) => type switch
    {
        VolumeType.Personal => """
            SELECT place.profile_addr, COUNT(*)::bigint AS expected
            FROM public.places place
            WHERE place.marketing_addr = @marketingAddr
              AND place.structure_number = @structureNumber
              AND place.profile_addr IS NOT NULL
              AND place.activated_at IS NOT NULL
            GROUP BY place.profile_addr
            """,
        VolumeType.Referral => """
            SELECT invite.parent_profile_addr AS profile_addr,
                   COUNT(*)::bigint AS expected
            FROM public.places place
            JOIN public.places invite
              ON invite.marketing_addr = place.marketing_addr
             AND invite.structure_number = 0
             AND invite.place_number = 1
             AND invite.profile_addr = place.profile_addr
            WHERE place.marketing_addr = @marketingAddr
              AND place.structure_number = @structureNumber
              AND place.profile_addr IS NOT NULL
              AND place.activated_at IS NOT NULL
              AND invite.parent_profile_addr IS NOT NULL
            GROUP BY invite.parent_profile_addr
            """,
        _ => throw new InvalidOperationException("Group volume recalculation is not implemented.")
    };

    private static string Column(VolumeType type) => type switch
    {
        VolumeType.Personal => "personal_volume",
        VolumeType.Referral => "referral_volume",
        _ => throw new InvalidOperationException("Group volume recalculation is not implemented.")
    };

    private static NpgsqlCommand Command(
        string sql,
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        RecalculationOptions options)
    {
        var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("marketingAddr", options.MarketingAddr);
        command.Parameters.AddWithValue("structureNumber", (short)options.StructureNumber);
        return command;
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

    private sealed record InspectionResult(long Profiles, long Mismatches);
}

using System.Security.Cryptography;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace ProgramPlaceParentRepairer;

internal sealed record ParentFields(string? Address, string? Login, long? Number);
internal sealed record ParentMismatch(
    string Marketing, int Id, short Structure, int? ParentId, ParentFields Current, ParentFields Expected, bool InvalidParent);
internal sealed record RepairTarget(string Marketing, short Structure);
internal sealed record ParentRepairProgress(string Phase, int Completed, int Total, RepairTarget Target, long Checked, int Mismatches);
internal sealed record ParentInspection(long Checked, IReadOnlyList<ParentMismatch> Mismatches, IReadOnlyList<RepairTarget> Targets)
{
    public int InvalidParents => Mismatches.Count(row => row.InvalidParent);
    public string Fingerprint => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { Targets, Mismatches })));
}

internal sealed class ParentRepairService(string connectionString)
{
    // parent_id is authoritative. A parent must belong to the same program and structure.
    private const string ScopeSql = """
        WITH scoped AS (
            SELECT child.*, parent.id AS resolved_parent_id,
                   parent.profile_addr AS expected_addr, parent.profile_login AS expected_login,
                   parent.place_number AS expected_number,
                   child.parent_id IS NOT NULL AND
                     (parent.id IS NULL OR parent.id = child.id) AS invalid_parent
            FROM public.places child
            LEFT JOIN public.places parent ON parent.id = child.parent_id
              AND parent.marketing_addr = child.marketing_addr
              AND parent.structure_number = child.structure_number
            WHERE child.marketing_addr = @marketing
              AND (@structure IS NULL OR child.structure_number = @structure)
        )
        """;
    private const string MismatchSql = """
        invalid_parent OR parent_profile_addr IS DISTINCT FROM expected_addr
          OR parent_profile_login IS DISTINCT FROM expected_login
          OR parent_place_number IS DISTINCT FROM expected_number
        """;

    public async Task<ParentInspection> RunAsync(
        string? marketing, short? structure, bool apply, CancellationToken ct, ParentInspection? expected = null,
        Action<ParentRepairProgress>? progress = null)
    {
        marketing = string.IsNullOrWhiteSpace(marketing) ? null : marketing.Trim();
        if (structure is < 0 or > 255) throw new ArgumentException("Structure must be between 0 and 255.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        NpgsqlCommand Command(string sql)
        {
            var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("marketing", NpgsqlDbType.Text, (object?)marketing ?? DBNull.Value);
            command.Parameters.AddWithValue("structure", NpgsqlDbType.Smallint, (object?)structure ?? DBNull.Value);
            return command;
        }
        if (apply)
        {
            await using var locks = Command("LOCK TABLE public.referal_program, public.structures, public.places IN SHARE ROW EXCLUSIVE MODE;");
            await locks.ExecuteNonQueryAsync(ct);
        }
        var targets = new List<RepairTarget>();
        await using (var targetCommand = Command("""
            SELECT structure.marketing_addr, structure.structure_number
            FROM public.structures structure
            JOIN public.referal_program program ON program.marketing_addr = structure.marketing_addr
            WHERE (@marketing IS NULL OR structure.marketing_addr = @marketing)
              AND (@structure IS NULL OR structure.structure_number = @structure)
            ORDER BY structure.marketing_addr, structure.structure_number;
            """))
        {
            await using var reader = await targetCommand.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                targets.Add(new RepairTarget(reader.GetString(0), reader.GetInt16(1)));
        }
        NpgsqlCommand TargetCommand(string sql, RepairTarget target)
        {
            var command = Command(sql);
            command.Parameters["marketing"].Value = target.Marketing;
            command.Parameters["structure"].Value = target.Structure;
            return command;
        }
        async Task<ParentInspection> Inspect(string phase)
        {
            long count = 0;
            var rows = new List<ParentMismatch>();
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                progress?.Invoke(new ParentRepairProgress(phase, index, targets.Count, target, count, rows.Count));
                await using (var counter = TargetCommand(ScopeSql + " SELECT COUNT(*) FROM scoped;", target))
                    count = checked(count + (long)(await counter.ExecuteScalarAsync(ct))!);
                await using var command = TargetCommand(ScopeSql + """
                    SELECT id, structure_number, parent_id, parent_profile_addr, parent_profile_login, parent_place_number,
                           expected_addr, expected_login, expected_number, invalid_parent
                    FROM scoped WHERE
                    """ + "\n" + MismatchSql + " ORDER BY id;", target);
                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    string? Text(int column) => reader.IsDBNull(column) ? null : reader.GetString(column);
                    long? Number(int column) => reader.IsDBNull(column) ? null : reader.GetInt64(column);
                    rows.Add(new ParentMismatch(target.Marketing, reader.GetInt32(0), reader.GetInt16(1),
                        reader.IsDBNull(2) ? null : reader.GetInt32(2),
                        new ParentFields(Text(3), Text(4), Number(5)),
                        new ParentFields(Text(6), Text(7), Number(8)), reader.GetBoolean(9)));
                }
                progress?.Invoke(new ParentRepairProgress(phase, index + 1, targets.Count, target, count, rows.Count));
            }
            return new ParentInspection(count, rows, targets);
        }
        var before = await Inspect(apply ? "Rechecking" : "Checking");
        if (!apply) return before;
        if (expected is null || before.Checked != expected.Checked || before.Fingerprint != expected.Fingerprint)
            throw new ParentRepairException("Inspection changed; run the check again before applying.");
        if (before.InvalidParents > 0)
            throw new ParentRepairException("Missing, self-referencing or out-of-scope parents found. No changes applied; fix parent_id manually first.");
        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index];
            progress?.Invoke(new ParentRepairProgress("Repairing", index, targets.Count, target, before.Checked, before.Mismatches.Count));
            await using var update = TargetCommand(ScopeSql + """
            UPDATE public.places child
            SET parent_profile_addr = scoped.expected_addr, parent_profile_login = scoped.expected_login,
                parent_place_number = scoped.expected_number
            FROM scoped WHERE child.id = scoped.id AND (
                scoped.parent_profile_addr IS DISTINCT FROM scoped.expected_addr
                OR scoped.parent_profile_login IS DISTINCT FROM scoped.expected_login
                OR scoped.parent_place_number IS DISTINCT FROM scoped.expected_number);
            """, target);
            await update.ExecuteNonQueryAsync(ct);
            progress?.Invoke(new ParentRepairProgress("Repairing", index + 1, targets.Count, target, before.Checked, before.Mismatches.Count));
        }
        var after = await Inspect("Verifying");
        if (after.Mismatches.Count != 0)
            throw new ParentRepairException("Verification failed; all changes rolled back.");
        await transaction.CommitAsync(ct);
        return before;
    }
}

internal sealed class ParentRepairException(string message) : Exception(message);

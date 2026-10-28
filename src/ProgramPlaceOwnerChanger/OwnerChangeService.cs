using Npgsql;
using NpgsqlTypes;

namespace ProgramPlaceOwnerChanger;

internal sealed class OwnerChangeService(string connectionString)
{
    internal sealed record Preview(long Places, long Locks, string? ParentAddress, string? ParentLogin);

    public async Task<Preview> RunAsync(OwnerChangeRequest request, bool apply, CancellationToken ct, Preview? expected = null)
    {
        if (request.OldAddress == request.NewAddress)
            throw new OwnerChangeException("Profiles must be different.");
        if (request.Structure is < 1 or > 255)
            throw new ArgumentException("Only paid structures can be transferred.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        NpgsqlCommand Command(string sql)
        {
            var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("marketing", request.Marketing);
            command.Parameters.AddWithValue("structure", NpgsqlDbType.Smallint, (object?)request.Structure ?? DBNull.Value);
            command.Parameters.AddWithValue("old", request.OldAddress);
            command.Parameters.AddWithValue("new", request.NewAddress);
            command.Parameters.AddWithValue("login", request.NewLogin);
            return command;
        }
        async Task Execute(string sql)
        {
            await using var command = Command(sql);
            await command.ExecuteNonQueryAsync(ct);
        }
        if (apply)
            await Execute("LOCK TABLE public.structures, public.places, public.locks, public.profile_volumes IN SHARE ROW EXCLUSIVE MODE;");

        await using (var validate = Command("""
            SELECT EXISTS (SELECT 1 FROM public.structures WHERE marketing_addr = @marketing
                           AND structure_number > 0 AND (@structure IS NULL OR structure_number = @structure));
            """))
        {
            if (!(bool)(await validate.ExecuteScalarAsync(ct))!)
                throw new OwnerChangeException("No target structures found.");
        }
        string? parentAddress;
        string? parentLogin;
        await using (var invite = Command("""
            SELECT parent_profile_addr, parent_profile_login FROM public.places
            WHERE marketing_addr = @marketing AND structure_number = 0 AND place_number = 1 AND profile_addr = @new;
            """))
        {
            await using var reader = await invite.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) throw new OwnerChangeException("New profile has no referral place.");
            parentAddress = reader.IsDBNull(0) ? null : reader.GetString(0);
            parentLogin = reader.IsDBNull(1) ? null : reader.GetString(1);
            if ((parentAddress is null) != (parentLogin is null)) throw new OwnerChangeException("Invalid referral parent.");
            if (await reader.ReadAsync(ct)) throw new OwnerChangeException("Ambiguous referral profile.");
        }
        Preview preview;
        await using (var counts = Command("""
            SELECT (SELECT COUNT(*) FROM public.places WHERE marketing_addr = @marketing AND structure_number > 0
                    AND (@structure IS NULL OR structure_number = @structure) AND profile_addr = @old),
                   (SELECT COUNT(*) FROM public.locks WHERE marketing_addr = @marketing AND structure_number > 0
                    AND (@structure IS NULL OR structure_number = @structure)
                    AND (profile_addr = @old OR place_profile_addr = @old));
            """))
        {
            await using var reader = await counts.ExecuteReaderAsync(ct);
            await reader.ReadAsync(ct);
            preview = new Preview(reader.GetInt64(0), reader.GetInt64(1), parentAddress, parentLogin);
        }
        if (!apply) return preview;
        if (expected != preview) throw new OwnerChangeException("Preview changed; run again.");

        await Execute("""
            CREATE TEMP TABLE owner_transfer ON COMMIT DROP AS
            SELECT source.id, source.structure_number, source.place_number AS old_number,
                   COALESCE((SELECT MAX(target.place_number) FROM public.places target
                             WHERE target.marketing_addr = @marketing
                               AND target.structure_number = source.structure_number
                               AND target.profile_addr = @new), 0)
                   + ROW_NUMBER() OVER (PARTITION BY source.structure_number ORDER BY source.place_number, source.id) AS new_number
            FROM public.places source
            WHERE source.marketing_addr = @marketing AND source.structure_number > 0
              AND (@structure IS NULL OR source.structure_number = @structure) AND source.profile_addr = @old;
            """);
        await using (var invalidParents = Command("""
            SELECT COUNT(*) FROM public.places child
            LEFT JOIN public.places parent ON parent.id = child.parent_id
            WHERE child.marketing_addr = @marketing
              AND child.structure_number > 0
              AND (@structure IS NULL OR child.structure_number = @structure)
              AND (child.id IN (SELECT id FROM owner_transfer) OR child.parent_id IN (SELECT id FROM owner_transfer))
              AND child.parent_id IS NOT NULL
              AND (parent.id IS NULL OR parent.id = child.id OR parent.marketing_addr <> child.marketing_addr
                   OR parent.structure_number <> child.structure_number);
            """))
        {
            if ((long)(await invalidParents.ExecuteScalarAsync(ct))! != 0)
                throw new OwnerChangeException("Invalid physical parent reference; transfer rolled back.");
        }
        await Execute("""
            UPDATE public.places place
            SET place_number = moved.new_number, profile_addr = @new, profile_login = @login,
                "index" = @login || moved.new_number::text
            FROM owner_transfer moved
            WHERE place.id = moved.id;

            WITH expected_parents AS (
                SELECT child.id, parent.profile_addr, parent.profile_login, parent.place_number
                FROM public.places child
                LEFT JOIN public.places parent ON parent.id = child.parent_id
                  AND parent.marketing_addr = child.marketing_addr AND parent.structure_number = child.structure_number
                WHERE child.marketing_addr = @marketing AND child.structure_number > 0
                  AND (@structure IS NULL OR child.structure_number = @structure)
                  AND (child.id IN (SELECT id FROM owner_transfer) OR child.parent_id IN (SELECT id FROM owner_transfer))
            )
            UPDATE public.places child
            SET parent_profile_addr = expected.profile_addr, parent_profile_login = expected.profile_login,
                parent_place_number = expected.place_number
            FROM expected_parents expected WHERE child.id = expected.id;

            WITH mapped AS (
                SELECT position_lock.id, moved.new_number
                FROM public.locks position_lock
                LEFT JOIN owner_transfer moved
                  ON position_lock.structure_number = moved.structure_number
                 AND position_lock.place_profile_addr = @old AND position_lock.place_number = moved.old_number
                WHERE position_lock.marketing_addr = @marketing AND position_lock.structure_number > 0
                  AND (@structure IS NULL OR position_lock.structure_number = @structure)
                  AND (position_lock.profile_addr = @old OR position_lock.place_profile_addr = @old)
            )
            UPDATE public.locks position_lock
            SET profile_addr = CASE WHEN profile_addr = @old THEN @new ELSE profile_addr END,
                place_profile_addr = CASE WHEN place_profile_addr = @old THEN @new ELSE place_profile_addr END,
                place_profile_login = CASE WHEN place_profile_addr = @old THEN @login ELSE place_profile_login END,
                place_number = COALESCE(mapped.new_number, position_lock.place_number)
            FROM mapped WHERE position_lock.id = mapped.id;

            UPDATE public.profile_volumes SET personal_volume = 0, referral_volume = 0
            WHERE marketing_addr = @marketing AND structure_number > 0
              AND (@structure IS NULL OR structure_number = @structure);

            WITH contributions AS (
                SELECT structure_number, profile_addr, COUNT(*)::bigint AS personal, 0::bigint AS referral
                FROM public.places
                WHERE marketing_addr = @marketing AND structure_number > 0
                  AND (@structure IS NULL OR structure_number = @structure)
                  AND profile_addr IS NOT NULL AND activated_at IS NOT NULL
                GROUP BY structure_number, profile_addr
                UNION ALL
                SELECT place.structure_number, invite.parent_profile_addr, 0::bigint, COUNT(*)::bigint
                FROM public.places place JOIN public.places invite
                  ON invite.marketing_addr = place.marketing_addr AND invite.structure_number = 0
                 AND invite.place_number = 1 AND invite.profile_addr = place.profile_addr
                WHERE place.marketing_addr = @marketing AND place.structure_number > 0
                  AND (@structure IS NULL OR place.structure_number = @structure)
                  AND place.activated_at IS NOT NULL AND invite.parent_profile_addr IS NOT NULL
                GROUP BY place.structure_number, invite.parent_profile_addr
            )
            INSERT INTO public.profile_volumes
                (marketing_addr, structure_number, profile_addr, personal_volume, referral_volume, group_volume)
            SELECT @marketing, structure_number, profile_addr, SUM(personal)::bigint, SUM(referral)::bigint, 0
            FROM contributions GROUP BY structure_number, profile_addr
            ON CONFLICT (marketing_addr, structure_number, profile_addr)
            DO UPDATE SET personal_volume = EXCLUDED.personal_volume, referral_volume = EXCLUDED.referral_volume;
            """);
        await using (var verification = Command("""
            SELECT COUNT(*) FROM public.places child
            LEFT JOIN public.places parent ON parent.id = child.parent_id
              AND parent.marketing_addr = child.marketing_addr AND parent.structure_number = child.structure_number
            WHERE child.marketing_addr = @marketing AND child.structure_number > 0
              AND (@structure IS NULL OR child.structure_number = @structure)
              AND (child.profile_addr = @old OR (
                (child.id IN (SELECT id FROM owner_transfer) OR child.parent_id IN (SELECT id FROM owner_transfer))
                AND (child.parent_profile_addr IS DISTINCT FROM parent.profile_addr
                     OR child.parent_profile_login IS DISTINCT FROM parent.profile_login
                     OR child.parent_place_number IS DISTINCT FROM parent.place_number)));
            """))
        {
            if ((long)(await verification.ExecuteScalarAsync(ct))! != 0)
                throw new OwnerChangeException("Verification failed; transfer rolled back.");
        }
        await transaction.CommitAsync(ct);
        return preview;
    }
}

internal sealed class OwnerChangeException(string message) : Exception(message);

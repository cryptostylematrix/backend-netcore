using System.Data;
using Npgsql;

namespace ProgramInviterChanger;

internal sealed class InviterChangeService(string connectionString)
{
    public async Task<InviterChangePlan> PlanAsync(
        string marketingAddr,
        string referralProfileAddr,
        string newInviterProfileAddr,
        CancellationToken cancellationToken)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await BuildPlanAsync(
            connection,
            transaction: null,
            marketingAddr,
            referralProfileAddr,
            newInviterProfileAddr,
            cancellationToken);
    }

    public async Task<InviterChangeResult> ChangeAsync(
        string marketingAddr,
        string referralProfileAddr,
        string newInviterProfileAddr,
        CancellationToken cancellationToken)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        await ExecuteAsync(
            connection,
            transaction,
            "LOCK TABLE public.places, public.locks IN SHARE ROW EXCLUSIVE MODE;",
            cancellationToken);

        var plan = await BuildPlanAsync(
            connection,
            transaction,
            marketingAddr,
            referralProfileAddr,
            newInviterProfileAddr,
            cancellationToken);
        if (plan.NoChange)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new InviterChangeResult(true, 0, 0);
        }

        var deepDelta = checked((long)plan.NewInviterDeep + 1L - plan.ReferralDeep);
        var movedPlaces = await UpdateSubtreeAsync(
            connection,
            transaction,
            marketingAddr,
            plan,
            deepDelta,
            cancellationToken);
        if (movedPlaces != plan.SubtreePlaces)
        {
            throw new InvalidOperationException(
                $"Expected to move {plan.SubtreePlaces} places, but updated {movedPlaces}.");
        }

        var updatedLocks = await UpdateSubtreeLocksAsync(
            connection,
            transaction,
            marketingAddr,
            plan.NewMp,
            cancellationToken);
        await RecalculateParentFillingAsync(
            connection,
            transaction,
            plan.OldParentId,
            plan.NewInviterId,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return new InviterChangeResult(false, movedPlaces, updatedLocks);
    }

    private static async Task<InviterChangePlan> BuildPlanAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string marketingAddr,
        string referralProfileAddr,
        string newInviterProfileAddr,
        CancellationToken cancellationToken)
    {
        var referral = await GetInvitePlaceAsync(
            connection,
            transaction,
            marketingAddr,
            referralProfileAddr,
            "referral",
            cancellationToken);
        var newInviter = await GetInvitePlaceAsync(
            connection,
            transaction,
            marketingAddr,
            newInviterProfileAddr,
            "new inviter",
            cancellationToken);

        if (referral.ParentId is null)
            throw new InvalidOperationException("The program root invite cannot be moved.");

        var currentParent = await GetPlaceByIdAsync(
            connection,
            transaction,
            referral.ParentId.Value,
            cancellationToken);

        if (referral.Id == newInviter.Id)
            throw new InvalidOperationException("A referral cannot be its own inviter.");

        if (referral.ParentId == newInviter.Id)
        {
            return new InviterChangePlan(
                true,
                referral.Id,
                referral.ParentId.Value,
                newInviter.Id,
                newInviter.ProfileAddr!,
                referral.ProfileLogin,
                currentParent.ProfileLogin,
                newInviter.ProfileLogin,
                referral.Mp,
                referral.Mp,
                referral.Deep,
                newInviter.Deep,
                referral.Pos,
                0);
        }

        var subtree = await GetSubtreeStatsAsync(
            connection,
            transaction,
            referral.Id,
            newInviter.Id,
            referral.Mp,
            cancellationToken);
        if (subtree.ContainsNewInviter)
            throw new InvalidOperationException("The new inviter belongs to the referral subtree.");
        if (!subtree.AllMpsValid)
            throw new InvalidOperationException("The referral subtree contains inconsistent MP values.");

        var maxPosition = await ScalarAsync<long>(
            connection,
            transaction,
            """
            SELECT COALESCE(MAX(pos), 0)::bigint
            FROM public.places
            WHERE parent_id = @newInviterId;
            """,
            [new NpgsqlParameter("newInviterId", newInviter.Id)],
            cancellationToken);
        if (maxPosition >= uint.MaxValue)
            throw new InvalidOperationException("The new inviter has no available position number.");

        var newPosition = checked((uint)(maxPosition + 1));
        var newMp = newInviter.Mp + newPosition.ToString("X8");
        var conflictingPlaces = await ScalarAsync<long>(
            connection,
            transaction,
            """
            SELECT COUNT(*)::bigint
            FROM public.places
            WHERE marketing_addr = @marketingAddr
              AND structure_number = 0
              AND LEFT(mp, LENGTH(@newMp)) = @newMp
              AND LEFT(mp, LENGTH(@oldMp)) <> @oldMp;
            """,
            [
                new NpgsqlParameter("marketingAddr", marketingAddr),
                new NpgsqlParameter("newMp", newMp),
                new NpgsqlParameter("oldMp", referral.Mp)
            ],
            cancellationToken);
        if (conflictingPlaces != 0)
            throw new InvalidOperationException("The destination MP range is already occupied.");

        return new InviterChangePlan(
            false,
            referral.Id,
            referral.ParentId.Value,
            newInviter.Id,
            newInviter.ProfileAddr!,
            referral.ProfileLogin,
            currentParent.ProfileLogin,
            newInviter.ProfileLogin,
            referral.Mp,
            newMp,
            referral.Deep,
            newInviter.Deep,
            newPosition,
            subtree.Count);
    }

    private static async Task<PlaceRow> GetInvitePlaceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string marketingAddr,
        string profileAddr,
        string role,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, parent_id, mp, pos, deep, profile_addr, profile_login, place_number
            FROM public.places
            WHERE marketing_addr = @marketingAddr
              AND structure_number = 0
              AND profile_addr = @profileAddr
              AND place_number = 1;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("marketingAddr", marketingAddr);
        command.Parameters.AddWithValue("profileAddr", profileAddr);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException($"The {role} has no invite in this program.");

        var row = new PlaceRow(
            reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetInt32(1),
            reader.GetString(2),
            checked((uint)reader.GetInt64(3)),
            checked((uint)reader.GetInt64(4)),
            reader.GetString(5),
            reader.GetString(6),
            checked((uint)reader.GetInt64(7)));
        if (await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException($"The {role} has multiple structure 0 places.");

        return row;
    }

    private static async Task<PlaceRow> GetPlaceByIdAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        int id,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, parent_id, mp, pos, deep, profile_addr, profile_login, place_number
            FROM public.places
            WHERE id = @id;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("The referral's current inviter was not found.");

        return new PlaceRow(
            reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetInt32(1),
            reader.GetString(2),
            checked((uint)reader.GetInt64(3)),
            checked((uint)reader.GetInt64(4)),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? "system" : reader.GetString(6),
            checked((uint)reader.GetInt64(7)));
    }

    private static async Task<SubtreeStats> GetSubtreeStatsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        int referralId,
        int newInviterId,
        string oldMp,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH RECURSIVE subtree AS
            (
                SELECT id, mp
                FROM public.places
                WHERE id = @referralId

                UNION ALL

                SELECT child.id, child.mp
                FROM public.places child
                JOIN subtree parent ON child.parent_id = parent.id
            )
            SELECT COUNT(*)::bigint,
                   COALESCE(BOOL_OR(id = @newInviterId), false),
                   COALESCE(BOOL_AND(LEFT(mp, LENGTH(@oldMp)) = @oldMp), true)
            FROM subtree;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("referralId", referralId);
        command.Parameters.AddWithValue("newInviterId", newInviterId);
        command.Parameters.AddWithValue("oldMp", oldMp);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new SubtreeStats(reader.GetInt64(0), reader.GetBoolean(1), reader.GetBoolean(2));
    }

    private static async Task<int> UpdateSubtreeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string marketingAddr,
        InviterChangePlan plan,
        long deepDelta,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH RECURSIVE subtree AS
            (
                SELECT id
                FROM public.places
                WHERE id = @referralId

                UNION ALL

                SELECT child.id
                FROM public.places child
                JOIN subtree parent ON child.parent_id = parent.id
            )
            UPDATE public.places place
            SET mp = @newMp || SUBSTRING(place.mp FROM LENGTH(@oldMp) + 1),
                deep = place.deep + @deepDelta,
                parent_id = CASE WHEN place.id = @referralId THEN @newInviterId ELSE place.parent_id END,
                parent_profile_addr = CASE WHEN place.id = @referralId THEN @newInviterProfileAddr ELSE place.parent_profile_addr END,
                parent_profile_login = CASE WHEN place.id = @referralId THEN @newInviterLogin ELSE place.parent_profile_login END,
                parent_place_number = CASE WHEN place.id = @referralId THEN @newInviterPlaceNumber ELSE place.parent_place_number END,
                pos = CASE WHEN place.id = @referralId THEN @newPosition ELSE place.pos END
            FROM subtree
            WHERE place.id = subtree.id
              AND place.marketing_addr = @marketingAddr
              AND place.structure_number = 0;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("referralId", plan.ReferralId);
        command.Parameters.AddWithValue("newInviterId", plan.NewInviterId);
        command.Parameters.AddWithValue("newInviterProfileAddr", plan.NewInviterProfileAddr);
        command.Parameters.AddWithValue("newInviterLogin", plan.NewInviterLogin);
        command.Parameters.AddWithValue("newInviterPlaceNumber", 1L);
        command.Parameters.AddWithValue("newPosition", checked((long)plan.NewPosition));
        command.Parameters.AddWithValue("oldMp", plan.OldMp);
        command.Parameters.AddWithValue("newMp", plan.NewMp);
        command.Parameters.AddWithValue("deepDelta", deepDelta);
        command.Parameters.AddWithValue("marketingAddr", marketingAddr);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> UpdateSubtreeLocksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string marketingAddr,
        string newMp,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE public.locks position_lock
            SET mp = place.mp || UPPER(LPAD(TO_HEX(position_lock.locked_pos), 8, '0'))
            FROM public.places place
            WHERE position_lock.marketing_addr = @marketingAddr
              AND position_lock.structure_number = 0
              AND place.marketing_addr = position_lock.marketing_addr
              AND place.structure_number = position_lock.structure_number
              AND place.profile_addr = position_lock.place_profile_addr
              AND place.place_number = position_lock.place_number
              AND LEFT(place.mp, LENGTH(@newMp)) = @newMp;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("marketingAddr", marketingAddr);
        command.Parameters.AddWithValue("newMp", newMp);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task RecalculateParentFillingAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int oldParentId,
        int newParentId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE public.places parent
            SET filling =
            (
                SELECT COUNT(*)::bigint
                FROM public.places child
                WHERE child.parent_id = parent.id
            )
            WHERE parent.id = ANY(@parentIds);
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("parentIds", new[] { oldParentId, newParentId });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<T> ScalarAsync<T>(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sql,
        IReadOnlyCollection<NpgsqlParameter> parameters,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var parameter in parameters)
            command.Parameters.Add(parameter);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return (T)(value ?? throw new InvalidOperationException("Database query returned no value."));
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

    private sealed record PlaceRow(
        int Id,
        int? ParentId,
        string Mp,
        uint Pos,
        uint Deep,
        string? ProfileAddr,
        string ProfileLogin,
        uint PlaceNumber);

    private sealed record SubtreeStats(long Count, bool ContainsNewInviter, bool AllMpsValid);
}

internal sealed record InviterChangePlan(
    bool NoChange,
    int ReferralId,
    int OldParentId,
    int NewInviterId,
    string NewInviterProfileAddr,
    string ReferralLogin,
    string OldInviterLogin,
    string NewInviterLogin,
    string OldMp,
    string NewMp,
    uint ReferralDeep,
    uint NewInviterDeep,
    uint NewPosition,
    long SubtreePlaces);

internal sealed record InviterChangeResult(bool NoChange, int MovedPlaces, int UpdatedLocks);

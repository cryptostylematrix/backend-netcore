using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Dto;

namespace ReferalProgram.Infrastructure.Queries;

public sealed class PlaceCommands(
    [FromKeyedServices("Programs")] NpgsqlDataSource dataSource) : IPlaceCommands
{
    public async Task<PlaceResponse> CreatePlaceAsync(
        CreatePlaceCommand command,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string updateInviterSql = """
            UPDATE public.places
            SET filling = filling + 1
            WHERE id = @ParentId
              AND filling = @ParentFilling;
            """;

        var updated = await connection.ExecuteAsync(
            new CommandDefinition(
                updateInviterSql,
                new
                {
                    command.ParentId,
                    ParentFilling = (long)command.ParentFilling
                },
                transaction,
                cancellationToken: cancellationToken));

        if (updated != 1)
            throw new InvalidOperationException("The inviter place changed while creating the invite.");

        const string insertPlaceSql = """
            INSERT INTO public.places
            (
                parent_id,
                marketing_addr,
                structure_number,
                profile_addr,
                profile_login,
                "index",
                place_number,
                parent_profile_addr,
                parent_profile_login,
                parent_place_number,
                mp,
                pos_group,
                kind,
                pos,
                filling,
                deep,
                is_active,
                created_at,
                activated_at,
                personal_volume,
                group_volume,
                task_key,
                task_query_id,
                task_source_addr
            )
            VALUES
            (
                @ParentId,
                @MarketingAddr,
                @StructureNumber,
                @ProfileAddr,
                @ProfileLogin,
                @Index,
                @PlaceNumber,
                @ParentProfileAddr,
                @ParentProfileLogin,
                @ParentPlaceNumber,
                @Mp,
                @PosGroup,
                @Kind,
                @Pos,
                @Filling,
                @Deep,
                @IsActive,
                @CreatedAt,
                @ActivatedAt,
                @PersonalVolume,
                @GroupVolume,
                @TaskKey,
                @TaskQueryId,
                @TaskSourceAddr
            )
            RETURNING
                id                    AS "Id",
                parent_id             AS "ParentId",
                mp                    AS "Mp",
                pos_group             AS "PosGroup",
                marketing_addr        AS "MarketingAddr",
                structure_number      AS "StructNumber",
                profile_addr          AS "ProfileAddr",
                place_number          AS "PlaceNumber",
                profile_login         AS "ProfileLogin",
                "index"               AS "Index",
                parent_profile_addr   AS "ParentProfileAddr",
                parent_profile_login  AS "ParentProfileLogin",
                parent_place_number   AS "ParentPlaceNumber",
                created_at            AS "CreatedAt",
                activated_at          AS "ActivatedAt",
                is_active             AS "IsActive",
                kind                  AS "Kind",
                pos                   AS "Pos",
                filling               AS "Filling",
                deep                  AS "Deep",
                personal_volume       AS "PersonalVolume",
                group_volume          AS "GroupVolume";
            """;

        var createdPlace = await connection.QuerySingleAsync<PlaceResponse>(
            new CommandDefinition(
                insertPlaceSql,
                new
                {
                    command.ParentId,
                    command.MarketingAddr,
                    StructureNumber = (short)command.StructureNumber,
                    command.ProfileAddr,
                    command.ProfileLogin,
                    command.Index,
                    PlaceNumber = (long)command.PlaceNumber,
                    command.ParentProfileAddr,
                    command.ParentProfileLogin,
                    ParentPlaceNumber = (long)command.ParentPlaceNumber,
                    command.Mp,
                    PosGroup = (short)command.PosGroup,
                    Kind = (short)command.Kind,
                    Pos = (long)command.Pos,
                    Filling = (long)command.Filling,
                    Deep = (long)command.Deep,
                    command.IsActive,
                    command.CreatedAt,
                    command.ActivatedAt,
                    PersonalVolume = (long)command.PersonalVolume,
                    GroupVolume = (long)command.GroupVolume,
                    command.TaskKey,
                    command.TaskQueryId,
                    command.TaskSourceAddr
                },
                transaction,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return createdPlace;
    }
}

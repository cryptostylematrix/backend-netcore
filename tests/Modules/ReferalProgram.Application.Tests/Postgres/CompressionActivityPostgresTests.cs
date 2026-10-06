using Microsoft.Extensions.DependencyInjection;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Infrastructure.Persistence;
using ReferalProgram.Infrastructure.Queries;
using ReferalProgram.Infrastructure.Repositories;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed partial class ActivityPostgresTests
{
    [DockerPostgresFact]
    public async Task Compression_persists_retained_inactive_chains_without_activation_or_volume_effects()
    {
        await using var db = await Database.Create();
        await db.Sql("""
            CREATE TABLE structure_ranks(marketing_addr text, structure_number smallint,
                name text, required_active_referral_places bigint)
            """);
        foreach (byte number in new byte[] { 0, 1 })
        foreach (var keep in new[] { false, true })
        foreach (var activeRoot in new[] { false, true })
        {
            await db.Reset(null);
            if (number == 1)
            {
                await db.InsertPlace(4, "program", 1, "owner", null, true);
                await db.InsertPlace(5, "program", 1, "inviter", 4, false);
                await db.InsertPlace(6, "program", 1, "member", 5, true);
            }
            var rootId = number == 0 ? 1 : 4;
            var inactiveId = rootId + 1;
            var childId = rootId + 2;
            await db.Sql("UPDATE places SET is_active=@activeRoot WHERE id=@rootId", new { activeRoot, rootId });
            await db.Sql("UPDATE places SET is_active=@activeRoot WHERE id=@childId", new { childId, activeRoot });
            await db.Sql("""
                UPDATE structures SET width=1,height=3,
                    activity=jsonb_build_object('type',@type,'when_inactive',jsonb_build_object('keep_on_compression',@keep)),
                    pos_algo='{"v":1,"root":"owner","relation":"absolute","groups":[{"id":0,"algo":"classic","weight":1}]}'
                WHERE structure_number=@number
                """, new { number = (short)number, type = number == 0 ? "invite" : "marketing", keep });
            var error = await db.Compress(number);
            Assert.Equal(activeRoot || keep, error is null);
            var root = (await db.Places.GetPlaceAsync(rootId, default))!;
            var inactive = await db.Places.GetPlaceAsync(inactiveId, default);
            var child = (await db.Places.GetPlaceAsync(childId, default))!;
            Assert.Equal(activeRoot, root.IsActive);
            Assert.Equal(activeRoot, child.IsActive);
            Assert.Null(root.ActivatedAt);
            Assert.Null(child.ActivatedAt);
            if (error is null)
            {
                Assert.Equal(keep, inactive is not null);
                Assert.Equal(keep ? inactiveId : rootId, child.ParentId);
                Assert.Equal(keep ? 3L : 2L, await db.Scalar("SELECT matrix_filling FROM places WHERE id=@rootId", new { rootId }));
                Assert.Equal(keep ? 3u : 2u, child.Deep);
            }
            else Assert.NotNull(inactive);
            if (inactive is not null)
            {
                Assert.False(inactive.IsActive);
                Assert.Null(inactive.ActivatedAt);
            }
            Assert.Equal(0L, await db.Count("profile_volumes"));
            Assert.Equal(0L, await db.Count("marketing_tasks"));
        }
    }

    private sealed partial class Database
    {
        public async Task<string?> Compress(byte number)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            return await new StructureCompressionService(scope.ServiceProvider.GetRequiredService<IPlaceRepository>(),
                new PositionLockRepository(context), Structures, new StructureRankQueries(data),
                new ProfileVolumeQueries(data), new PositionAlgorithmConfigurationParser(), context, Places)
                .CompressAsync("program", number, default);
        }
    }
}

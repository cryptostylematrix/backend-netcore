using ReferalProgram.Infrastructure.Queries;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed partial class ActivityPostgresTests
{
    [DockerPostgresFact]
    public async Task Statistics_count_invite_descendants_across_structures()
    {
        await using var db = await Database.Create();
        await db.VerifyStatistics();
    }

    private sealed partial class Database
    {
        public async Task VerifyStatistics()
        {
            await Reset(null);
            await InsertPlace(4, "program", 0, "deep", 3, true);
            await InsertPlace(5, "program", 0, "sibling", 1, true);
            await InsertPlace(6, "program", 1, "member", null, false);
            await InsertPlace(7, "program", 1, "deep", null, true);
            await InsertPlace(8, "program", 1, "inviter", null, true);
            await InsertPlace(9, "program", 1, "sibling", 8, true);
            await InsertPlace(10, "other-program", 1, "member", null, true);
            await InsertPlace(11, "program", 1, "clone-owner", null, false);
            await Sql("UPDATE places SET profile_addr='member',place_number=2,kind=1,activated_at=1 WHERE id=11");
            await Sql("UPDATE places SET activated_at=1 WHERE id IN (3,7)");
            await Sql("INSERT INTO structures VALUES ('program',2,1,2,0,2,false,'{}',NULL,NULL)");
            var queries = new ProgramStatisticsQueries(data);
            var result = await queries.GetAsync("program", "inviter", default);
            Assert.NotNull(result);
            Assert.Equal(1, result.Referrals.Total);
            Assert.Equal(0, result.Referrals.Active);
            Assert.Equal(1, result.Referrals.Activated);
            Assert.Equal(3, result.Structures.Count());
            var invites = result.Structures.Single(s => s.StructureNumber == 0);
            Assert.Equal(2, invites.TotalProfiles);
            Assert.Equal(2, invites.TotalPlaces);
            Assert.Equal(1, invites.ActiveProfiles);
            Assert.Equal(1, invites.ActivatedProfiles);
            var paid = result.Structures.Single(s => s.StructureNumber == 1);
            Assert.Equal(2, paid.TotalProfiles);
            Assert.Equal(3, paid.TotalPlaces);
            Assert.Equal(1, paid.ActivePlaces);
            Assert.Equal(1, paid.ActiveProfiles);
            Assert.Equal(2, paid.ActivatedPlaces);
            Assert.Equal(2, paid.ActivatedProfiles);
            Assert.Equal(1, paid.Referrals.Total);
            Assert.Equal(2, paid.Referrals.TotalPlaces);
            Assert.Equal(0, paid.Referrals.Active);
            Assert.Equal(1, paid.Referrals.Activated);
            Assert.Equal(0, result.Structures.Single(s => s.StructureNumber == 2).TotalPlaces);
            var leaf = await queries.GetAsync("program", "deep", default);
            Assert.NotNull(leaf);
            Assert.Equal(0, leaf.Referrals.Total);
            Assert.All(leaf.Structures, s => { Assert.Equal(0, s.TotalPlaces); Assert.Equal(0, s.TotalProfiles); });
            Assert.Null(await queries.GetAsync("program", "missing", default));
        }
    }
}

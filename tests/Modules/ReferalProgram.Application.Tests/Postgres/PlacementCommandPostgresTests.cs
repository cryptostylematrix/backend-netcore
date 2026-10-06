using Ardalis.Result;
using Microsoft.Extensions.DependencyInjection;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Places;
using ReferalProgram.Application.Policies;
using ReferalProgram.Application.Services;
using ReferalProgram.Application.Services.RootStrategies;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Dto;
using ReferalProgram.Infrastructure.Persistence;
using ReferalProgram.Infrastructure.Queries;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed partial class ActivityPostgresTests
{
    [DockerPostgresFact]
    public async Task Placement_purchase_clone_and_reinvest_handlers_commit_only_when_own_placement_is_allowed()
    {
        await using var db = await Database.Create();
        foreach (var operation in new[] { PositionOperation.BuyFirstPlace, PositionOperation.BuyPlace,
            PositionOperation.CreateClone, PositionOperation.CreateReinvest })
        foreach (var allow in new[] { false, true })
        {
            await db.Reset(null);
            await db.InsertPlace(4, "program", 1, "inviter", null, false);
            if (operation == PositionOperation.BuyPlace)
                await db.InsertPlace(5, "program", 1, "member", 4, false);
            await db.Sql("UPDATE places SET is_active=true WHERE id=3");
            await db.Sql("""
                UPDATE structures SET max_places_per_profile=0,
                  activity=jsonb_build_object('type','marketing','when_inactive',jsonb_build_object('allow_own_children',@allow)),
                  pos_algo='{"v":1,"root":"owner","relation":"relative","groups":[{"id":0,"algo":"radar","weight":1}]}'
                WHERE structure_number=1
                """, new { allow });
            var result = await db.PlaceCommand(operation);
            Assert.True(result.IsSuccess == allow, $"{operation}: {string.Join("; ", result.Errors)}");
            Assert.Equal(allow ? 1L : 0L, await db.Count("marketing_tasks"));
            Assert.Equal(allow ? 2L : 0L, await db.Scalar("SELECT COALESCE(sum(personal_volume+referral_volume),0) FROM profile_volumes"));
        }
    }

    [DockerPostgresFact]
    public async Task Placement_manual_purchase_rechecks_activity_and_persists_nothing_on_denial()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        await db.InsertPlace(4, "program", 1, "member", null, true);
        await db.InsertPlace(5, "program", 1, "other", 4, false);
        await db.InsertPlace(6, "program", 0, "other", 1, false);
        await db.Sql("""
            UPDATE structures SET max_places_per_profile=0,
              activity='{"type":"marketing","when_inactive":{"check_manual_placement":true},"spillover":{"allow_inactive_place":true,"require_active_invite":true}}',
              pos_algo='{"v":1,"root":"owner","relation":"relative","groups":[{"id":0,"algo":"classic","weight":1}]}'
            WHERE structure_number=1
            """);
        var position = new ChildPosition(new BuyPlaceRef(1, "other", 1), 1);
        var denied = await db.PlaceCommand(PositionOperation.BuyPlace, position);
        Assert.False(denied.IsSuccess);
        Assert.Contains("parent_activity_disallows_placement", string.Join("; ", denied.Errors));
        Assert.Equal(0L, await db.Count("marketing_tasks"));
        Assert.Equal(0L, await db.Count("profile_volumes"));
        Assert.Equal(0L, await db.Scalar("SELECT filling FROM places WHERE id=5"));
        await db.Sql("UPDATE places SET is_active=true WHERE id=6");
        var allowed = await db.PlaceCommand(PositionOperation.BuyPlace, position);
        Assert.True(allowed.IsSuccess, string.Join("; ", allowed.Errors));
        Assert.Equal(5, (await db.Places.GetPlaceAsync("program", 1, "member", 2, default))?.ParentId);
        Assert.Equal(1L, await db.Count("marketing_tasks"));
        Assert.Equal(1L, await db.Scalar("SELECT filling FROM places WHERE id=5"));
    }

    private sealed partial class Database
    {
        public async Task<Result<CommandResponse>> PlaceCommand(PositionOperation operation, ChildPosition? position = null, ushort relativeLevel = 0)
        {
            await using var scope = provider.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPlaceRepository>();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var locks = new LockQueries(data);
            var next = new NextPosService(new NextPositionQueries(Structures, Places),
                new PositionAlgorithmConfigurationParser(), new PositionGroupSelector(),
                new PositionRootResolver([new OwnerRootPlaceStrategy(Places)]),
                new PositionAlgorithmResolver(Algorithms(Places)), locks, Places);
            if (operation is PositionOperation.CreateClone or PositionOperation.CreateReinvest)
                return await new CreateSystemCloneCommandHandler(repo, Structures, new RelativePlaceResolver(Places, Structures),
                    next, new ClonePlaceKindPolicy(repo), new SourcePlaceResolver(repo), context)
                    .Handle(new("program", 1, 0, "member", 1, relativeLevel, 1, 1, operation), default);
            var policy = new BuyPlacePolicy(Structures, Places, locks, next, new PurchaseCommands(), new RequestedPositionResolver(Places));
            return await new BuyPlaceCommandHandler(repo, Structures, policy, new SourcePlaceResolver(repo), context)
                .Handle(new("program", 1, "member", "member", 1, 1, "payer-is-not-the-child",
                    operation == PositionOperation.BuyFirstPlace ? BuyPlaceKind.First : BuyPlaceKind.Regular, position), default);
        }
    }

    private sealed class PurchaseCommands : IProgramCommandQueries
    {
        public Task<ProgramCommandConfiguration> GetConfigurationAsync(string marketingAddr, CancellationToken cancellationToken) =>
            Task.FromResult(new ProgramCommandConfiguration(new Dictionary<byte, IReadOnlySet<uint>>
            { [1] = new HashSet<uint> { ProgramCommandTags.BuyFirstPlace, ProgramCommandTags.BuyPlace } }));
    }
}

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
    public async Task Manual_buy_branches_allow_second_branch_and_reject_foreign_locked_or_stale_positions()
    {
        await using var db = await Database.Create();
        await db.SetupManualBuyBranches();
        var policy = db.ManualBranchPolicy();
        var foreign = await db.ManualBranchPurchase(new(new BuyPlaceRef(1, "other", 1), 2), 1);
        Assert.False(foreign.IsSuccess);
        Assert.Contains("position_is_outside_viewer_root", string.Join("; ", foreign.Errors));
        var second = (await db.Places.GetPlaceAsync(7, default))!;
        await db.Sql("""
            INSERT INTO locks (task_key,task_query_id,marketing_addr,structure_number,
                place_profile_addr,place_number,place_profile_login,profile_addr,locked_pos,mp,created_at)
            VALUES (99,99,'program',1,'member',2,'member','member',2,@mp,1)
            """, new { mp = second.Mp + "00000002" });
        var locked = await db.ManualBranchPurchase(new(new BuyPlaceRef(1, "member", 2), 2), 2);
        Assert.False(locked.IsSuccess);
        Assert.Contains("position_is_locked", string.Join("; ", locked.Errors));
        Assert.Equal(0L, await db.Count("marketing_tasks"));
        await db.Sql("DELETE FROM locks");
        var automatic = await policy.EvaluateAsync("program", 1, "member", null, default);
        Assert.True(automatic.CanBuy);
        Assert.NotNull(automatic.Position);
        Assert.StartsWith((await db.Places.GetPlaceAsync(5, default))!.Mp, automatic.Position.Mp);
        var option = await policy.EvaluateAsync("program", 1, "member", new(1, "member", 2, 2), default);
        Assert.True(option.CanBuy);
        Assert.True(policy.EvaluatePosition(option, second, second.Mp + "00000002", 2, false).CanBuy);
        var bought = await db.ManualBranchPurchase(new(new BuyPlaceRef(1, "member", 2), 2), 3);
        Assert.True(bought.IsSuccess, string.Join("; ", bought.Errors));
        Assert.Equal(7, (await db.Places.GetPlaceAsync("program", 1, "member", 4, default))!.ParentId);
        Assert.Equal(1L, await db.Count("marketing_tasks"));
        var stale = await db.ManualBranchPurchase(new(new BuyPlaceRef(1, "member", 2), 2), 4);
        Assert.False(stale.IsSuccess);
        Assert.Equal(1L, await db.Count("marketing_tasks"));
    }

    [DockerPostgresFact]
    public async Task Manual_buy_branches_keep_secondary_selection_when_first_branch_is_full_without_automatic_fallback()
    {
        await using var db = await Database.Create();
        await db.SetupManualBuyBranches();
        await db.InsertPlace(10, "program", 1, "terminal", 5, true);
        await db.Sql("UPDATE places SET kind=2 WHERE id IN (8,10)");
        var policy = db.ManualBranchPolicy();
        var decision = await policy.EvaluateAsync("program", 1, "member", null, default);
        Assert.True(decision.CanBuy);
        Assert.True(decision.IncludePosition);
        Assert.Null(decision.Position);
        var second = (await db.Places.GetPlaceAsync(7, default))!;
        Assert.True(policy.EvaluatePosition(decision, second, second.Mp + "00000002", 2, false).CanBuy);
        var bought = await db.ManualBranchPurchase(new(new BuyPlaceRef(1, "member", 2), 2), 1);
        Assert.True(bought.IsSuccess, string.Join("; ", bought.Errors));
        Assert.Equal(7, (await db.Places.GetPlaceAsync("program", 1, "member", 4, default))!.ParentId);
    }

    private sealed partial class Database
    {
        public async Task SetupManualBuyBranches()
        {
            await SetupLockBranches();
            await Sql("""
                UPDATE structures SET max_places_per_profile=0,
                    pos_algo='{"v":1,"root":"profile","relation":"relative","groups":[{"id":0,"algo":"classic","weight":1}]}'
                WHERE marketing_addr='program' AND structure_number=1;
                """);
        }

        public BuyPlacePolicy ManualBranchPolicy()
        {
            var locks = new LockQueries(data);
            var next = new NextPosService(new NextPositionQueries(Structures, Places),
                new PositionAlgorithmConfigurationParser(), new PositionGroupSelector(),
                new PositionRootResolver([new ProfileRootPlaceStrategy(new ProfileRootPlaceResolver(Places, Structures))]),
                new PositionAlgorithmResolver(Algorithms(Places)), locks, Places);
            return new BuyPlacePolicy(Structures, Places, locks, next, new PurchaseCommands(), new RequestedPositionResolver(Places));
        }

        public async Task<Result<CommandResponse>> ManualBranchPurchase(ChildPosition position, int key)
        {
            await using var scope = provider.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPlaceRepository>();
            return await new BuyPlaceCommandHandler(repo, Structures, ManualBranchPolicy(), new SourcePlaceResolver(repo),
                scope.ServiceProvider.GetRequiredService<DataContext>())
                .Handle(new("program", 1, "member", "member", key, key, "any-payer", BuyPlaceKind.Regular, position), default);
        }
    }
}

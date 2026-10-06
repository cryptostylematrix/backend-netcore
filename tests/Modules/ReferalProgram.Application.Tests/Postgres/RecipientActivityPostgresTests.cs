using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Places;
using ReferalProgram.Application.Services;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed partial class ActivityPostgresTests
{
    [DockerPostgresFact]
    public async Task Bonus_handlers_use_the_recipient_structure_and_preserve_reason_and_state()
    {
        await using var db = await Database.Create();
        foreach (var inviteBonus in new[] { false, true })
        foreach (var marketingBonus in new[] { false, true })
        {
            await db.Reset(null);
            await db.InsertPlace(4, "program", 1, "owner", null, true);
            await db.InsertPlace(5, "program", 1, "inviter", 4, false);
            await db.InsertPlace(6, "program", 1, "member", 5, true);
            await db.Sql("UPDATE places SET is_active=true WHERE id=3");
            await db.RecipientSettings(0, inviteBonus, !inviteBonus);
            await db.RecipientSettings(1, marketingBonus, !marketingBonus);
            var resolver = new RelativePlaceResolver(db.Places, db.Structures);
            var handler = new ResolveBonusQueryHandler(db.Places, resolver);
            foreach (var tag in new uint[] { 0xe1319040, 0x1b5547d5 })
            {
                var result = await handler.Handle(new("program", tag, 1, "member", 1, 1), default);
                Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
                Assert.Equal(marketingBonus ? "inviter" : "owner", result.Value.RecipientProfileAddr);
                Assert.Equal(6, result.Value.Reason.Id);
            }
            var referral = await handler.Handle(new("program", 0xb5ce6bf5, 1, "member", 1, 0), default);
            Assert.True(referral.IsSuccess, string.Join("; ", referral.Errors));
            Assert.Equal(inviteBonus ? "inviter" : "owner", referral.Value.RecipientProfileAddr);
            Assert.Equal(6, referral.Value.Reason.Id);
            Assert.Equal(0L, await db.Count("marketing_tasks"));
            Assert.Equal(0L, await db.Count("profile_volumes"));
            Assert.False((await db.Places.GetPlaceAsync(2, default))!.IsActive);
            Assert.False((await db.Places.GetPlaceAsync(5, default))!.IsActive);
        }
    }

    [DockerPostgresFact]
    public async Task Clone_and_reinvest_resolve_their_own_recipient_and_commit_receipt_and_volume()
    {
        await using var db = await Database.Create();
        foreach (var clone in new[] { false, true })
        foreach (var operation in new[] { PositionOperation.CreateClone, PositionOperation.CreateReinvest })
        {
            await db.Reset(null);
            await db.InsertPlace(4, "program", 1, "owner", null, true);
            await db.Sql("UPDATE places SET is_active=true WHERE id=3");
            await db.RecipientSettings(0, !clone, clone);
            await db.Sql("""
                UPDATE structures SET max_places_per_profile=0,
                pos_algo='{"v":1,"root":"owner","relation":"relative","groups":[{"id":0,"algo":"classic","weight":1}]}'
                WHERE structure_number=1
                """);
            var result = await db.PlaceCommand(operation, relativeLevel: 1);
            Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
            var recipient = clone ? "inviter" : "owner";
            var created = await db.Places.GetPlaceAsync("program", 1, recipient, clone ? 1u : 2u, default);
            Assert.NotNull(created);
            Assert.True(created.IsActive);
            Assert.NotNull(created.ActivatedAt);
            Assert.Equal(1L, await db.Count("marketing_tasks"));
            Assert.Equal(1L, await db.Scalar("SELECT personal_volume FROM profile_volumes WHERE profile_addr=@recipient AND structure_number=1", new { recipient }));
            // A first paid place retains the existing invite-activation side effect.
            Assert.Equal(clone, (await db.Places.GetPlaceAsync(2, default))!.IsActive);
        }
    }

    [DockerPostgresFact]
    public async Task Combined_task_uses_clone_eligibility_for_branch_and_bonus_eligibility_for_payment()
    {
        await using var db = await Database.Create();
        foreach (var bonus in new[] { false, true })
        foreach (var clone in new[] { false, true })
        foreach (var hasTarget in new[] { false, true })
        {
            await db.Reset(null);
            await db.Sql("UPDATE places SET is_active=true WHERE id=3");
            await db.InsertPlace(4, "program", 1, "owner", null, true);
            if (hasTarget) await db.InsertPlace(5, "program", 1, "inviter", 4, true);
            await db.RecipientSettings(0, bonus, clone);
            var resolver = new RelativePlaceResolver(db.Places, db.Structures);
            var decision = await new ResolveMoveOrStructBonusQueryHandler(db.Places, resolver)
                .Handle(new("program", 1, 0, "member", 1, 1), default);
            Assert.True(decision.IsSuccess, string.Join("; ", decision.Errors));
            Assert.Equal(clone && !hasTarget, decision.Value.CreateClone);
            if (!decision.Value.CreateClone)
            {
                var payment = await new ResolveBonusQueryHandler(db.Places, resolver)
                    .Handle(new("program", 0xe1319040, 0, "member", 1, 1), default);
                Assert.True(payment.IsSuccess);
                Assert.Equal(bonus ? "inviter" : "owner", payment.Value.RecipientProfileAddr);
            }
        }
    }

    [DockerPostgresFact]
    public async Task Combined_task_can_fall_back_to_bonus_when_no_clone_recipient_exists()
    {
        await using var db = await Database.Create();
        await db.Reset(null);
        await db.Sql("UPDATE places SET is_active=false");
        var resolver = new RelativePlaceResolver(db.Places, db.Structures);
        var handler = new ResolveMoveOrStructBonusQueryHandler(db.Places, resolver);
        await db.RecipientSettings(0, bonus: false, clone: false);
        Assert.False((await handler.Handle(new("program", 1, 0, "member", 1, 1), default)).IsSuccess);
        await db.RecipientSettings(0, bonus: true, clone: false);
        var result = await handler.Handle(new("program", 1, 0, "member", 1, 1), default);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.CreateClone);
        var payment = await new ResolveBonusQueryHandler(db.Places, resolver)
            .Handle(new("program", 0xe1319040, 0, "member", 1, 1), default);
        Assert.True(payment.IsSuccess);
        Assert.Equal("inviter", payment.Value.RecipientProfileAddr);
        Assert.Equal(0L, await db.Count("marketing_tasks"));
    }

    private sealed partial class Database
    {
        public Task RecipientSettings(byte number, bool bonus, bool clone) => Sql("""
            UPDATE structures SET activity=jsonb_build_object('type',@type,'when_inactive',
                jsonb_build_object('allow_as_bonus_recipient',@bonus,'allow_as_clone_recipient',@clone))
            WHERE marketing_addr='program' AND structure_number=@number
            """, new { number = (short)number, type = number == 0 ? "invite" : "marketing", bonus, clone });
    }
}

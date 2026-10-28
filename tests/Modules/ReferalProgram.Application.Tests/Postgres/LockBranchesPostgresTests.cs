using Ardalis.Result;
using Contracts.Application.Features.ProfileItem;
using Contracts.Dto;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Locks;
using ReferalProgram.Application.Policies;
using ReferalProgram.Core.LockAggregate;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Dto;
using ReferalProgram.Infrastructure.Persistence;
using ReferalProgram.Infrastructure.Queries;
using ReferalProgram.Infrastructure.Repositories;

namespace ReferalProgram.Application.Tests.Postgres;

public sealed partial class ActivityPostgresTests
{
    [DockerPostgresFact]
    public async Task Lock_branch_roots_exclude_nested_places_and_preserve_scope_and_mp_case()
    {
        await using var db = await Database.Create();
        await db.SetupLockBranches();
        var first = (await db.Places.GetPlaceAsync(5, default))!.Mp;
        var second = (await db.Places.GetPlaceAsync(7, default))!.Mp;
        // Foreign program/structure ancestors must not remove this program's roots.
        await db.InsertPlace(10, "other-program", 1, "member", null, true);
        await db.InsertPlace(11, "program", 2, "member", null, true);
        await db.Sql("UPDATE places SET mp='00000004' WHERE id IN (10,11)");
        await db.InsertPlace(12, "program", 1, "upper", null, false);
        await db.InsertPlace(13, "program", 1, "lower", null, false);
        await db.Sql("""
            UPDATE places SET profile_addr='member',place_number=4,mp='0000000A' WHERE id=12;
            UPDATE places SET profile_addr='member',place_number=5,mp='0000000a00000001' WHERE id=13;
            """);
        var roots = await db.Places.GetProfileBranchRootMpsAsync("program", 1, "member", default);
        Assert.Equal(new[] { first, second, "0000000A", "0000000a00000001" }, roots);
        Assert.Empty(await db.Places.GetProfileBranchRootMpsAsync("program", 1, "missing", default));
        Assert.Empty(await db.Places.GetProfileBranchRootMpsAsync("program", 1, " ", default));
    }

    [DockerPostgresFact]
    public async Task Lock_commands_allow_both_owned_branches_and_protect_each_root()
    {
        await using var db = await Database.Create();
        await db.SetupLockBranches();
        foreach (var number in new uint[] { 1, 2 })
        {
            var locked = await db.ChangeLock(false, "member", number, 2, (int)number);
            Assert.True(locked.IsSuccess, string.Join("; ", locked.Errors));
        }
        Assert.Equal(2L, await db.Count("locks"));
        var firstRoot = await db.ChangeLock(false, "owner", 1, 1, 3);
        var secondRoot = await db.ChangeLock(false, "other", 1, 1, 4);
        Assert.False(firstRoot.IsSuccess);
        Assert.False(secondRoot.IsSuccess);
        Assert.Contains("viewer_root_cannot_be_locked", string.Join("; ", firstRoot.Errors));
        Assert.Contains("viewer_root_cannot_be_locked", string.Join("; ", secondRoot.Errors));
        foreach (var number in new uint[] { 1, 2 })
        {
            var unlocked = await db.ChangeLock(true, "member", number, 2, 10 + (int)number);
            Assert.True(unlocked.IsSuccess, string.Join("; ", unlocked.Errors));
        }
        Assert.Equal(0L, await db.Count("locks"));
        Assert.Equal(4L, await db.Count("marketing_tasks"));
    }

    [DockerPostgresFact]
    public async Task Lock_commands_deny_foreign_branches_even_with_existing_lock_and_deny_non_owners()
    {
        await using var db = await Database.Create();
        await db.SetupLockBranches();
        var deniedOwner = await db.ChangeLock(false, "member", 2, 2, 1, wallet: "someone-else");
        Assert.False(deniedOwner.IsSuccess);
        var noPlaces = await db.ChangeLock(false, "member", 2, 2, 2, profile: "inviter");
        Assert.False(noPlaces.IsSuccess);
        // A lock inherited from a former owner can now lie outside every owned branch.
        var other = (await db.Places.GetPlaceAsync(6, default))!;
        await db.Sql("""
            INSERT INTO locks (task_key,task_query_id,task_source_addr,marketing_addr,structure_number,
                place_profile_addr,place_number,place_profile_login,profile_addr,locked_pos,mp,created_at)
            VALUES (99,99,'wallet','program',1,'other',1,'other','member',2,@mp,1)
            """, new { mp = other.Mp + "00000002" });
        foreach (var unlock in new[] { false, true })
        {
            var result = await db.ChangeLock(unlock, "other", 1, 2, unlock ? 4 : 3);
            Assert.False(result.IsSuccess);
            Assert.Contains("position_is_outside_viewer_root", string.Join("; ", result.Errors));
        }
        Assert.Equal(1L, await db.Count("locks"));
        Assert.Equal(0L, await db.Count("marketing_tasks"));
    }

    private sealed partial class Database
    {
        public async Task SetupLockBranches()
        {
            await Reset(null);
            await InsertPlace(4, "program", 1, "owner", null, true);
            await InsertPlace(5, "program", 1, "member", 4, true);
            await InsertPlace(6, "program", 1, "other", 4, true);
            await InsertPlace(7, "program", 1, "imported", 6, false);
            await InsertPlace(8, "program", 1, "nested", 5, false);
            await InsertPlace(9, "program", 1, "child", 7, true);
            await Sql("""
                UPDATE places SET profile_addr='member',profile_login='member',place_number=2 WHERE id=7;
                UPDATE places SET profile_addr='member',profile_login='member',place_number=3 WHERE id=8;
                """);
        }

        public async Task<Result<CommandResponse>> ChangeLock(
            bool unlock, string placeProfile, uint number, uint position, int key,
            string wallet = "wallet", string profile = "member")
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var places = scope.ServiceProvider.GetRequiredService<IPlaceRepository>();
            var repository = new PositionLockRepository(context);
            var sender = new LockProfileSender();
            var addresses = new LockAddresses();
            var queries = new LockQueries(data);
            var policy = new PositionLockPolicy();
            return unlock
                ? await new UnlockPositionCommandHandler(Places, Structures, sender, addresses,
                    queries, policy, places, repository, context)
                    .Handle(new("program", 1, 1, profile, placeProfile, number, position, key, key, wallet), default)
                : await new LockPositionCommandHandler(Places, Structures, sender, addresses,
                    queries, policy, places, repository, context)
                    .Handle(new("program", 1, 1, profile, placeProfile, number, position, key, key, wallet), default);
        }
    }

    private sealed class LockAddresses : ITonAddressComparer
    {
        public bool AreEqual(string? left, string? right) => left is not null && left == right;
    }

    private sealed class LockProfileSender : ISender
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Assert.IsType<GetNftDataQuery>(request);
            return Task.FromResult((TResponse)(object)Result.Success(new ProfileDataResponse { OwnerAddr = "wallet" }));
        }
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
            throw new NotSupportedException();
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

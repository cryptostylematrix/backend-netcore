using Common.Domain;
using UI.Application.Abstractions;
using UI.Application.Features.ProfileIntents;
using UI.Application.Services;
using UI.Core.ProfileAggregate;
using UI.Core.ProfileAggregate.Events;
using UI.Core.WalletProfileIntentAggregate;
using UI.Core.WalletProfileIntentAggregate.Events;
using UI.Dto;
using Xunit;

namespace UI.Application.Tests;

public sealed class ProfileIntentHandlersTests
{
    private static readonly DateTime Now =
        new(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Adds_owner_intent_when_wallet_owns_profile()
    {
        var context = Context(ownerAddr: "wallet");
        var handler = context.AddHandler();

        var result = await handler.Handle(
            new AddProfileIntentCommand(" wallet ", " Alice ", ProfileModeResponse.Owner),
            default);

        Assert.True(result.Value.Success);
        var intent = Assert.Single(context.Intents.Items);
        Assert.Equal(WalletProfileMode.Owner, intent.Mode);
        Assert.True(intent.Owned);
        Assert.Contains(intent.DomainEvents,
            item => item is WalletProfileIntentAddedDomainEvent);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Rejects_owner_mode_and_offers_preview_for_non_owner()
    {
        var context = Context(ownerAddr: "another-wallet");

        var result = await context.AddHandler().Handle(
            new AddProfileIntentCommand("wallet", "alice", ProfileModeResponse.Owner),
            default);

        Assert.False(result.Value.Success);
        Assert.Contains(UiErrorCodes.ProfileDoesNotBelongToWallet, result.Value.Errors);
        Assert.Equal([ProfileModeResponse.Preview], result.Value.AvailableModes);
        Assert.Empty(context.Intents.Items);
        Assert.Single(context.Profiles.Items);
    }

    [Fact]
    public async Task Repeated_add_is_idempotent_and_does_not_add_history_event()
    {
        var context = Context(ownerAddr: "another-wallet");
        var intent = WalletProfileIntent.Add(
            "wallet",
            "profile",
            WalletProfileMode.Preview,
            owned: false,
            Now);
        intent.ClearDomainEvents();
        context.Intents.Items.Add(intent);

        var result = await context.AddHandler().Handle(
            new AddProfileIntentCommand("wallet", "alice", ProfileModeResponse.Preview),
            default);

        Assert.True(result.Value.Success);
        Assert.Single(context.Intents.Items);
        Assert.Empty(intent.DomainEvents);
    }

    [Fact]
    public async Task Existing_owner_intent_records_ownership_loss()
    {
        var context = Context(ownerAddr: "another-wallet");
        var intent = WalletProfileIntent.Add(
            "wallet",
            "profile",
            WalletProfileMode.Owner,
            owned: true,
            Now.AddDays(-1));
        intent.ClearDomainEvents();
        context.Intents.Items.Add(intent);

        var result = await context.AddHandler().Handle(
            new AddProfileIntentCommand("wallet", "alice", ProfileModeResponse.Owner),
            default);

        Assert.False(result.Value.Success);
        Assert.False(intent.Owned);
        Assert.Contains(intent.DomainEvents,
            item => item is WalletProfileOwnershipLostDomainEvent);
    }

    [Fact]
    public async Task Removes_intent_and_records_removal()
    {
        var context = Context(ownerAddr: "wallet");
        context.Profiles.Items.Add(CachedProfile.Create(
            "profile",
            "alice",
            "{\"login\":\"alice\"}",
            Now.AddDays(-1)));
        var intent = WalletProfileIntent.Add(
            "wallet",
            "profile",
            WalletProfileMode.Owner,
            owned: true,
            Now.AddDays(-1));
        intent.ClearDomainEvents();
        context.Intents.Items.Add(intent);

        var result = await context.RemoveHandler().Handle(
            new RemoveProfileIntentCommand("wallet", "alice"),
            default);

        Assert.True(result.Value.Success);
        Assert.Empty(context.Intents.Items);
        Assert.Contains(intent.DomainEvents,
            item => item is WalletProfileIntentRemovedDomainEvent);
    }

    [Fact]
    public async Task Check_refreshes_content_and_records_ownership_loss()
    {
        var context = Context(
            ownerAddr: "another-wallet",
            contentJson: "{\"login\":\"alice\",\"image_url\":\"new\"}");
        var profile = CachedProfile.Create(
            "profile",
            "alice",
            "{\"login\":\"alice\",\"image_url\":\"old\"}",
            Now.AddDays(-1));
        context.Profiles.Items.Add(profile);
        var intent = WalletProfileIntent.Add(
            "wallet",
            "profile",
            WalletProfileMode.Owner,
            owned: true,
            Now.AddDays(-1));
        intent.ClearDomainEvents();
        context.Intents.Items.Add(intent);

        var result = await context.CheckHandler().Handle(
            new CheckWalletProfilesCommand("wallet"),
            default);

        Assert.True(result.Value.Success);
        Assert.False(intent.Owned);
        Assert.Contains(intent.DomainEvents,
            item => item is WalletProfileOwnershipLostDomainEvent);
        Assert.Contains(profile.DomainEvents,
            item => item is ProfileContentChangedDomainEvent);
    }

    [Fact]
    public async Task Check_marks_previously_unowned_profile_as_owned()
    {
        var context = Context(ownerAddr: "wallet");
        var profile = CachedProfile.Create(
            "profile",
            "alice",
            "{\"login\":\"alice\"}",
            Now.AddDays(-1));
        context.Profiles.Items.Add(profile);
        var intent = WalletProfileIntent.Add(
            "wallet",
            "profile",
            WalletProfileMode.Preview,
            owned: false,
            Now.AddDays(-1));
        intent.ClearDomainEvents();
        context.Intents.Items.Add(intent);

        var result = await context.CheckHandler().Handle(
            new CheckWalletProfilesCommand("wallet"),
            default);

        Assert.True(result.Value.Success);
        Assert.True(intent.Owned);
        Assert.Contains(intent.DomainEvents,
            item => item is WalletProfileOwnershipGainedDomainEvent);
        Assert.DoesNotContain(intent.DomainEvents,
            item => item is WalletProfileIntentAddedDomainEvent);
        Assert.Equal(1, context.UnitOfWork.SaveCount);
    }

    private static TestContext Context(
        string? ownerAddr,
        string contentJson = "{\"login\":\"alice\"}")
    {
        var profiles = new ProfileRepositoryStub();
        return new TestContext(
            profiles,
            new IntentRepositoryStub(profiles),
            new UnitOfWorkStub(),
            new AddressServiceStub(),
            new ContractReaderStub(new ProfileContractSnapshot(
                "profile",
                "alice",
                ownerAddr,
                contentJson)),
            new QueriesStub(),
            new FixedTimeProvider(Now));
    }

    private sealed record TestContext(
        ProfileRepositoryStub Profiles,
        IntentRepositoryStub Intents,
        UnitOfWorkStub UnitOfWork,
        AddressServiceStub Addresses,
        ContractReaderStub Contracts,
        QueriesStub Queries,
        FixedTimeProvider Clock)
    {
        private ProfileSynchronizer Synchronizer =>
            new(Contracts, Profiles, Clock);

        public AddProfileIntentCommandHandler AddHandler() =>
            new(Addresses, Synchronizer, Intents, UnitOfWork, Clock);

        public RemoveProfileIntentCommandHandler RemoveHandler() =>
            new(Addresses, Intents, UnitOfWork, Clock);

        public CheckWalletProfilesCommandHandler CheckHandler() =>
            new(
                Addresses,
                Synchronizer,
                Profiles,
                Intents,
                Queries,
                UnitOfWork,
                Clock);
    }

    private sealed class ProfileRepositoryStub : ICachedProfileRepository
    {
        public List<CachedProfile> Items { get; } = [];

        public Task<CachedProfile?> GetByAddressAsync(
            string address,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Address == address));

        public void Add(CachedProfile profile) => Items.Add(profile);
    }

    private sealed class IntentRepositoryStub(ProfileRepositoryStub profiles)
        : IWalletProfileIntentRepository
    {
        public List<WalletProfileIntent> Items { get; } = [];

        public Task<WalletProfileIntent?> GetAsync(
            string walletAddr,
            string profileAddr,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item =>
                item.WalletAddr == walletAddr && item.ProfileAddr == profileAddr));

        public Task<WalletProfileIntent?> GetByLoginAsync(
            string walletAddr,
            string profileLogin,
            CancellationToken cancellationToken)
        {
            var profile = profiles.Items.SingleOrDefault(item =>
                item.Login == profileLogin);
            return Task.FromResult(profile is null
                ? null
                : Items.SingleOrDefault(item =>
                    item.WalletAddr == walletAddr
                    && item.ProfileAddr == profile.Address));
        }

        public Task<IReadOnlyCollection<WalletProfileIntent>> ListAsync(
            string walletAddr,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<WalletProfileIntent>>(
                Items.Where(item => item.WalletAddr == walletAddr).ToArray());

        public void Add(WalletProfileIntent intent) => Items.Add(intent);
        public void Remove(WalletProfileIntent intent) => Items.Remove(intent);
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public void Dispose()
        {
        }
    }

    private sealed class AddressServiceStub : IWalletAddressService
    {
        public bool TryNormalize(string? address, out string normalizedAddress)
        {
            normalizedAddress = address?.Trim() ?? string.Empty;
            return normalizedAddress.Length > 0;
        }

        public bool AreEqual(string? left, string? right) =>
            string.Equals(left?.Trim(), right?.Trim(), StringComparison.Ordinal);

        public IReadOnlyCollection<string> GetEquivalentRepresentations(
            string normalizedAddress) => [normalizedAddress];
    }

    private sealed class ContractReaderStub(ProfileContractSnapshot snapshot)
        : IProfileContractReader
    {
        public Task<ProfileContractLookup> GetByLoginAsync(
            string login,
            CancellationToken cancellationToken) =>
            Task.FromResult(ProfileContractLookup.Success(snapshot));
    }

    private sealed class QueriesStub : IWalletProfileQueries
    {
        public Task<IReadOnlyCollection<WalletProfileResponse>> ListAsync(
            string walletAddr,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<WalletProfileResponse>>([]);
    }

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}

using UI.Application.EventHandlers;
using UI.Core.WalletProfileIntentAggregate;
using UI.Core.WalletProfileIntentAggregate.Events;
using Xunit;

namespace UI.Application.Tests;

public sealed class WalletProfileIntentEventHandlersTests
{
    private static readonly DateTime OccurredAt =
        new(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Added_event_is_appended_to_history()
    {
        var repository = new EventRepositoryStub();
        var handler = new WalletProfileIntentAddedDomainEventHandler(repository);

        await handler.Handle(new WalletProfileIntentAddedDomainEvent(
            Guid.NewGuid(),
            OccurredAt,
            "wallet",
            "profile",
            WalletProfileMode.Owner,
            true), default);

        var eventItem = Assert.Single(repository.Items);
        Assert.Equal(WalletProfileIntentEventType.Added, eventItem.EventType);
        Assert.Contains("\"mode\":\"owner\"", eventItem.DataJson);
        Assert.Contains("\"owned\":true", eventItem.DataJson);
    }

    [Fact]
    public async Task Removed_event_is_appended_to_history()
    {
        var repository = new EventRepositoryStub();
        var handler = new WalletProfileIntentRemovedDomainEventHandler(repository);

        await handler.Handle(new WalletProfileIntentRemovedDomainEvent(
            Guid.NewGuid(),
            OccurredAt,
            "wallet",
            "profile",
            WalletProfileMode.Preview,
            false), default);

        Assert.Equal(
            WalletProfileIntentEventType.Removed,
            Assert.Single(repository.Items).EventType);
    }

    [Fact]
    public async Task Ownership_loss_is_appended_to_history()
    {
        var repository = new EventRepositoryStub();
        var handler = new WalletProfileOwnershipLostDomainEventHandler(repository);

        await handler.Handle(new WalletProfileOwnershipLostDomainEvent(
            Guid.NewGuid(),
            OccurredAt,
            "wallet",
            "profile",
            WalletProfileMode.Owner), default);

        var eventItem = Assert.Single(repository.Items);
        Assert.Equal(
            WalletProfileIntentEventType.OwnershipLost,
            eventItem.EventType);
        Assert.Contains("\"owned\":false", eventItem.DataJson);
    }

    [Fact]
    public async Task Ownership_gain_is_appended_to_history_without_an_added_event()
    {
        var repository = new EventRepositoryStub();
        var handler = new WalletProfileOwnershipGainedDomainEventHandler(repository);

        await handler.Handle(new WalletProfileOwnershipGainedDomainEvent(
            Guid.NewGuid(),
            OccurredAt,
            "wallet",
            "profile",
            WalletProfileMode.Preview), default);

        var eventItem = Assert.Single(repository.Items);
        Assert.Equal(
            WalletProfileIntentEventType.OwnershipGained,
            eventItem.EventType);
        Assert.Contains("\"owned\":true", eventItem.DataJson);
    }

    private sealed class EventRepositoryStub : IWalletProfileIntentEventRepository
    {
        public List<WalletProfileIntentEvent> Items { get; } = [];

        public void Add(WalletProfileIntentEvent eventItem) => Items.Add(eventItem);
    }
}

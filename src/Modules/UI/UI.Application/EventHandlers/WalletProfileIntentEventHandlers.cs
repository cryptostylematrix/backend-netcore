using System.Text.Json;
using UI.Core.WalletProfileIntentAggregate;
using UI.Core.WalletProfileIntentAggregate.Events;

namespace UI.Application.EventHandlers;

internal sealed class WalletProfileIntentAddedDomainEventHandler(
    IWalletProfileIntentEventRepository repository)
    : IDomainEventHandler<WalletProfileIntentAddedDomainEvent>
{
    public Task Handle(
        WalletProfileIntentAddedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        repository.Add(WalletProfileIntentEvent.Create(
            notification.Id,
            notification.WalletAddr,
            notification.ProfileAddr,
            WalletProfileIntentEventType.Added,
            JsonSerializer.Serialize(new
            {
                mode = notification.Mode.ToString().ToLowerInvariant(),
                owned = notification.Owned
            }),
            notification.OccurredOnUtc));
        return Task.CompletedTask;
    }
}

internal sealed class WalletProfileIntentRemovedDomainEventHandler(
    IWalletProfileIntentEventRepository repository)
    : IDomainEventHandler<WalletProfileIntentRemovedDomainEvent>
{
    public Task Handle(
        WalletProfileIntentRemovedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        repository.Add(WalletProfileIntentEvent.Create(
            notification.Id,
            notification.WalletAddr,
            notification.ProfileAddr,
            WalletProfileIntentEventType.Removed,
            JsonSerializer.Serialize(new
            {
                mode = notification.Mode.ToString().ToLowerInvariant(),
                owned = notification.Owned
            }),
            notification.OccurredOnUtc));
        return Task.CompletedTask;
    }
}

internal sealed class WalletProfileOwnershipLostDomainEventHandler(
    IWalletProfileIntentEventRepository repository)
    : IDomainEventHandler<WalletProfileOwnershipLostDomainEvent>
{
    public Task Handle(
        WalletProfileOwnershipLostDomainEvent notification,
        CancellationToken cancellationToken)
    {
        repository.Add(WalletProfileIntentEvent.Create(
            notification.Id,
            notification.WalletAddr,
            notification.ProfileAddr,
            WalletProfileIntentEventType.OwnershipLost,
            JsonSerializer.Serialize(new
            {
                mode = notification.Mode.ToString().ToLowerInvariant(),
                owned = false
            }),
            notification.OccurredOnUtc));
        return Task.CompletedTask;
    }
}

internal sealed class WalletProfileOwnershipGainedDomainEventHandler(
    IWalletProfileIntentEventRepository repository)
    : IDomainEventHandler<WalletProfileOwnershipGainedDomainEvent>
{
    public Task Handle(
        WalletProfileOwnershipGainedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        repository.Add(WalletProfileIntentEvent.Create(
            notification.Id,
            notification.WalletAddr,
            notification.ProfileAddr,
            WalletProfileIntentEventType.OwnershipGained,
            JsonSerializer.Serialize(new
            {
                mode = notification.Mode.ToString().ToLowerInvariant(),
                owned = true
            }),
            notification.OccurredOnUtc));
        return Task.CompletedTask;
    }
}

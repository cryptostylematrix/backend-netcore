using Common.Domain;
using MediatR;

namespace UI.Infrastructure.Persistence;

internal sealed class DomainEventDispatcher(IPublisher publisher)
    : IDomainEventDispatcher
{
    public async Task DispatchAndClearEventsAsync(
        IEnumerable<IEntity> entities,
        CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            var events = entity.DomainEvents.ToArray();
            entity.ClearDomainEvents();

            foreach (var domainEvent in events)
                await publisher.Publish(domainEvent, cancellationToken);
        }
    }
}

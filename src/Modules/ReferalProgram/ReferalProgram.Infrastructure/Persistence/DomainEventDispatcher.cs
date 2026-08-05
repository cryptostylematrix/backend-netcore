using Common.Domain;
using MediatR;

namespace ReferalProgram.Infrastructure.Persistence;

internal sealed class DomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAndClearEventsAsync(
        IEnumerable<IEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = entities
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToArray();

        var domainEvents = entitiesWithEvents
            .SelectMany(entity => entity.DomainEvents)
            .ToArray();

        foreach (var entity in entitiesWithEvents)
            entity.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await publisher.Publish(domainEvent, cancellationToken);
    }
}

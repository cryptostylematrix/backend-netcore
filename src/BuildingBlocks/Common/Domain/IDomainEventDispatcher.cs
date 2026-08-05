namespace Common.Domain;

public interface IDomainEventDispatcher
{
    Task DispatchAndClearEventsAsync(
        IEnumerable<IEntity> entities,
        CancellationToken cancellationToken = default);
}

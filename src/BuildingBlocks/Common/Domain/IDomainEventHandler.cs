using MediatR;

namespace Common.Domain;

public interface IDomainEventHandler<in TDomainEvent> 
    : INotificationHandler<TDomainEvent> where TDomainEvent : DomainEvent;
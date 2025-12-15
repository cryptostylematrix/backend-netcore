using MediatR;

namespace Common.Domain;

public interface IDomainEvent : INotification
{
    Guid Id { get; init; }

    DateTime OccurredOnUtc { get; init; }
}
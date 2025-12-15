namespace Common.Domain;

public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent(Guid id, DateTime occuredOnUtc)
    {
        Id = id;
        OccurredOnUtc = occuredOnUtc;
    }

    private DomainEvent()
    {
    }

    public Guid Id { get; init; }

    public DateTime OccurredOnUtc { get; init; }
}
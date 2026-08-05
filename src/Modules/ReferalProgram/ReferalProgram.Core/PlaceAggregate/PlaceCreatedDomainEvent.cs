using Common.Domain;

namespace ReferalProgram.Core.PlaceAggregate;

public sealed record PlaceCreatedDomainEvent : DomainEvent
{
    public PlaceCreatedDomainEvent(
        int parentId,
        uint expectedParentFilling)
        : base(Guid.NewGuid(), DateTime.UtcNow)
    {
        ParentId = parentId;
        ExpectedParentFilling = expectedParentFilling;
    }

    public int ParentId { get; }
    public uint ExpectedParentFilling { get; }
}

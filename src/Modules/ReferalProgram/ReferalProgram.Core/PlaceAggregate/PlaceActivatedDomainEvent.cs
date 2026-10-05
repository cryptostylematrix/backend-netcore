using Common.Domain;

namespace ReferalProgram.Core.PlaceAggregate;

public sealed record PlaceActivatedDomainEvent : DomainEvent
{
    public PlaceActivatedDomainEvent(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        uint placeNumber,
        long activatedAt)
        : base(Guid.NewGuid(), DateTimeOffset.FromUnixTimeSeconds(activatedAt).UtcDateTime)
    {
        MarketingAddr = marketingAddr;
        StructureNumber = structureNumber;
        ProfileAddr = profileAddr;
        PlaceNumber = placeNumber;
        ActivatedAt = activatedAt;
    }

    public string MarketingAddr { get; }
    public byte StructureNumber { get; }
    public string ProfileAddr { get; }
    public uint PlaceNumber { get; }
    public long ActivatedAt { get; }
}

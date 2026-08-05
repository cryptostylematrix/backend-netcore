using Common.Domain;

namespace ReferalProgram.Core.PlaceAggregate;

public sealed record PlaceBoughtDomainEvent : DomainEvent
{
    public PlaceBoughtDomainEvent(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        uint placeNumber,
        long boughtAt)
        : base(Guid.NewGuid(), DateTimeOffset.FromUnixTimeSeconds(boughtAt).UtcDateTime)
    {
        MarketingAddr = marketingAddr;
        StructureNumber = structureNumber;
        ProfileAddr = profileAddr;
        PlaceNumber = placeNumber;
        BoughtAt = boughtAt;
    }

    public string MarketingAddr { get; }
    public byte StructureNumber { get; }
    public string ProfileAddr { get; }
    public uint PlaceNumber { get; }
    public long BoughtAt { get; }
}

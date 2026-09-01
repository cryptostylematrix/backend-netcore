using Common.Domain;
using ReferalProgram.Core.ProfileVolumeAggregate;

namespace ReferalProgram.Core.PlaceAggregate;

public sealed record ProfileVolumeOperationDomainEvent : DomainEvent
{
    public ProfileVolumeOperationDomainEvent(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        ProfileVolumeOperation operation,
        long occurredAt)
        : base(Guid.NewGuid(), DateTimeOffset.FromUnixTimeSeconds(occurredAt).UtcDateTime)
    {
        MarketingAddr = marketingAddr;
        StructureNumber = structureNumber;
        ProfileAddr = profileAddr;
        Operation = operation;
    }

    public string MarketingAddr { get; }
    public byte StructureNumber { get; }
    public string ProfileAddr { get; }
    public ProfileVolumeOperation Operation { get; }
}

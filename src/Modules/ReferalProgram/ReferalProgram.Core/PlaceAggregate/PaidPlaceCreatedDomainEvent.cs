using Common.Domain;

namespace ReferalProgram.Core.PlaceAggregate;

public sealed record PaidPlaceCreatedDomainEvent : DomainEvent
{
    public PaidPlaceCreatedDomainEvent(
        string marketingAddr,
        string profileAddr,
        long createdAt)
        : base(Guid.NewGuid(), DateTimeOffset.FromUnixTimeSeconds(createdAt).UtcDateTime)
    {
        MarketingAddr = marketingAddr;
        ProfileAddr = profileAddr;
        CreatedAt = createdAt;
    }

    public string MarketingAddr { get; }
    public string ProfileAddr { get; }
    public long CreatedAt { get; }
}

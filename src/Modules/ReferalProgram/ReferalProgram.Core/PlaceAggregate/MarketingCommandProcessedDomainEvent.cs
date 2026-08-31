using Common.Domain;

namespace ReferalProgram.Core.PlaceAggregate;

public sealed record MarketingCommandProcessedDomainEvent : DomainEvent
{
    public MarketingCommandProcessedDomainEvent(
        string marketingAddr,
        int taskKey,
        long taskQueryId,
        string? taskSourceAddr,
        Place place,
        Place responseSourcePlace,
        uint responseCode,
        DateTimeOffset processedAt)
        : base(Guid.NewGuid(), processedAt.UtcDateTime)
    {
        MarketingAddr = marketingAddr;
        TaskKey = taskKey;
        TaskQueryId = taskQueryId;
        TaskSourceAddr = taskSourceAddr;
        Place = place;
        ResponseSourcePlace = responseSourcePlace;
        ResponseCode = responseCode;
        ProcessedAt = processedAt;
    }

    public string MarketingAddr { get; }
    public int TaskKey { get; }
    public long TaskQueryId { get; }
    public string? TaskSourceAddr { get; }
    public Place Place { get; }
    public Place ResponseSourcePlace { get; }
    public uint ResponseCode { get; }
    public DateTimeOffset ProcessedAt { get; }
}

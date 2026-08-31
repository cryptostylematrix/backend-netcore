using Common.Domain;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Core.MarketingTaskAggregate;

public sealed class MarketingTask : Entity, IAggregateRoot
{
    private MarketingTask()
    {
    }

    private MarketingTask(
        string marketingAddr,
        int taskKey,
        long taskQueryId,
        string? taskSourceAddr,
        Place place,
        Place responseSourcePlace,
        uint responseCode,
        DateTimeOffset createdAt)
    {
        MarketingAddr = marketingAddr;
        TaskKey = taskKey;
        TaskQueryId = taskQueryId;
        TaskSourceAddr = taskSourceAddr;
        Place = place;
        ResponseSourcePlace = responseSourcePlace;
        ResponseCode = responseCode;
        CreatedAt = createdAt;
    }

    public string MarketingAddr { get; private set; } = null!;
    public int TaskKey { get; private set; }
    public long TaskQueryId { get; private set; }
    public string? TaskSourceAddr { get; private set; }
    public int PlaceId { get; private set; }
    public Place Place { get; private set; } = null!;
    public int ResponseSourcePlaceId { get; private set; }
    public Place ResponseSourcePlace { get; private set; } = null!;
    public uint ResponseCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static MarketingTask RecordProcessedCommand(
        string marketingAddr,
        int taskKey,
        long taskQueryId,
        string? taskSourceAddr,
        Place place,
        Place responseSourcePlace,
        uint responseCode,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(marketingAddr);
        ArgumentNullException.ThrowIfNull(place);
        ArgumentNullException.ThrowIfNull(responseSourcePlace);

        if (taskKey <= 0)
            throw new ArgumentOutOfRangeException(nameof(taskKey));

        if (taskQueryId < 0)
            throw new ArgumentOutOfRangeException(nameof(taskQueryId));

        if (!string.Equals(place.MarketingAddr, marketingAddr, StringComparison.Ordinal)
            || !string.Equals(
                responseSourcePlace.MarketingAddr,
                marketingAddr,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The command place and response source must belong to the task marketing.");
        }

        return new MarketingTask(
            marketingAddr,
            taskKey,
            taskQueryId,
            taskSourceAddr,
            place,
            responseSourcePlace,
            responseCode,
            createdAt);
    }
}

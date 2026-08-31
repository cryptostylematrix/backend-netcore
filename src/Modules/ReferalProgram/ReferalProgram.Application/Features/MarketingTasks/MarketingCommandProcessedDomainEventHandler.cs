using Common.Domain;
using ReferalProgram.Core.MarketingTaskAggregate;
using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Features.MarketingTasks;

internal sealed class MarketingCommandProcessedDomainEventHandler(
    IMarketingTaskRepository repository)
    : IDomainEventHandler<MarketingCommandProcessedDomainEvent>
{
    public Task Handle(
        MarketingCommandProcessedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        repository.Add(MarketingTask.RecordProcessedCommand(
            notification.MarketingAddr,
            notification.TaskKey,
            notification.TaskQueryId,
            notification.TaskSourceAddr,
            notification.Place,
            notification.ResponseSourcePlace,
            notification.ResponseCode,
            notification.ProcessedAt));

        return Task.CompletedTask;
    }
}

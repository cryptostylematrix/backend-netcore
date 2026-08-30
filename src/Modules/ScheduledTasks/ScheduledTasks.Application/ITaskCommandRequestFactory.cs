using MessageBroker.Abstractions;

namespace ScheduledTasks.Application;

public interface ITaskCommandRequestFactory
{
    bool CanCreate(TaskCommandEnvelope command);

    IIntegrationRequest Create(
        TaskCommandEnvelope command,
        Guid correlationId,
        DateTime occurredOnUtc);
}

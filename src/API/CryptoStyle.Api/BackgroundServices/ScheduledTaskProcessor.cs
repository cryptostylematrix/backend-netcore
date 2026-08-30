using ScheduledTasks.Application;

namespace CryptoStyle.Api.BackgroundServices;

public sealed class ScheduledTaskProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<ScheduledTaskProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedAny = await DrainDueTasksAsync(stoppingToken);
                if (!processedAny)
                    await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Scheduled-task polling cycle failed");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    private async Task<bool> DrainDueTasksAsync(CancellationToken cancellationToken)
    {
        var processedAny = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<IScheduledTaskRunner>();
            if (!await runner.RunNextAsync(cancellationToken))
                return processedAny;

            processedAny = true;
        }

        return processedAny;
    }
}

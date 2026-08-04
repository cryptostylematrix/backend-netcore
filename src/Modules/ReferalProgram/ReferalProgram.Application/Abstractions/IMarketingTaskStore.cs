namespace ReferalProgram.Application.Abstractions;

public interface IMarketingTaskStore
{
    Task<bool> HasIncompleteAsync(CancellationToken cancellationToken);

    Task<bool> TryAddAsync(
        int taskKey,
        long taskQueryId,
        CancellationToken cancellationToken);
}

using ScheduledTasks.Application.Abstractions;
using System.Text.Json;

namespace ScheduledTasks.Application;

public interface IMarketingTaskBlocker
{
    Task<bool> IsBlockedAsync(
        string marketingAddress,
        CancellationToken cancellationToken);
}

public sealed class MarketingTaskBlocker(
    IScheduledTaskQueries queries,
    TaskCommandDocumentParser parser,
    TimeProvider timeProvider) : IMarketingTaskBlocker
{
    public async Task<bool> IsBlockedAsync(
        string marketingAddress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(marketingAddress);

        var documents = await queries.GetDueTaskCommandDocumentsAsync(
            timeProvider.GetUtcNow(),
            cancellationToken);
        foreach (var document in documents)
        {
            try
            {
                if (parser.GetProgramMarketingAddresses(document).Contains(
                    marketingAddress,
                    StringComparer.Ordinal))
                {
                    return true;
                }
            }
            catch (Exception exception) when (exception is JsonException or FormatException)
            {
                // The execution worker records malformed due tasks as errors. A malformed
                // target cannot safely be attributed to a particular marketing here.
            }
        }

        return false;
    }
}

namespace UI.Core.TonConnectionAggregate;

public interface ITonConnectionRepository
{
    // Atomic insert/update, including concurrent first connections. Every accepted snapshot refreshes the last connection time.
    Task UpsertAsync(TonConnection connection, CancellationToken cancellationToken);
}

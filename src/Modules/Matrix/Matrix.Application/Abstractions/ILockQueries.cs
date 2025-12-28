namespace Matrix.Application.Abstractions;

public interface ILockQueries
{
    Task<string[]> GetAllLockMpsAsync(
        short m,
        string profileAddr,
        CancellationToken ct);

    Task<Paginated<LockResponse>> GetLocksAsync(
        short m,
        string profileAddr,
        int page,
        int pageSize,
        CancellationToken ct);
}
namespace ReferalProgram.Application.Abstractions;

public interface ILockQueries
{
    Task<string[]> GetAllLockMpsAsync(
        string marketingAddr,
        byte structNumber,
        string? profileAddr,
        CancellationToken ct);

    Task<Paginated<LockResponse>> GetLocksAsync(
        string marketingAddr,
        byte structNumber,
        string profileAddr,
        int page,
        int pageSize,
        CancellationToken ct);
}

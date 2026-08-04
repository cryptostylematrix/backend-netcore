namespace ReferalProgram.Application.Abstractions;

public interface ILockQueries
{
    Task<Paginated<LockResponse>> GetLocksAsync(
        string marketingAddr,
        byte structNumber,
        string profileAddr,
        int page,
        int pageSize,
        CancellationToken ct);
}

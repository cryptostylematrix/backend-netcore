namespace Marketing.Application.Abstractions;

public interface INextPosService
{
    Task<NextPosResponse?> GetNextPosAsync(string marketingAddr, byte m, string profileAddr, CancellationToken ct);
}
namespace Matrix.Application.Abstractions;

public interface INextPosService
{
    Task<NextPosResponse?> GetNextPosAsync(short m, string profileAddr, CancellationToken ct);
}
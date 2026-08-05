namespace ReferalProgram.Application.Abstractions;

public interface INextPosService
{
    Task<NextPosResponse?> GetNextPosAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken ct);
}

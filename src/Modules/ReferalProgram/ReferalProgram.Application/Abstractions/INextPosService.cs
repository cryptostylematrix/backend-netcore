namespace ReferalProgram.Application.Abstractions;

public sealed record PositionSelection(
    string Algorithm,
    PositionAlgorithmStrategyContext Context);

public interface INextPosService
{
    Task<PositionSelection?> ResolveSelectionAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        PositionOperation? operation,
        CancellationToken ct);

    Task<NextPosResponse?> FindNextAsync(
        PositionSelection selection,
        CancellationToken ct);

    Task<NextPosResponse?> GetNextPosAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        PositionOperation? operation,
        CancellationToken ct);
}

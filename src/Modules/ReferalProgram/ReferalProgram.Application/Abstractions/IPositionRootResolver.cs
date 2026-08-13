namespace ReferalProgram.Application.Abstractions;

public interface IPositionRootResolver
{
    Task<PlaceResponse?> ResolveAsync(
        string strategyName,
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken);
}

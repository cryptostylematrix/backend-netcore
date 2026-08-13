namespace ReferalProgram.Application.Abstractions;

public interface IProfileRootPlaceResolver
{
    Task<PlaceResponse?> ResolveAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken);
}

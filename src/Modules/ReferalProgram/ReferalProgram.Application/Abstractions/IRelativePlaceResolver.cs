namespace ReferalProgram.Application.Abstractions;

public sealed record RelativePlaceResolution(
    PlaceResponse SourcePlace,
    PlaceResponse RelativePlace);

public interface IRelativePlaceResolver
{
    Task<RelativePlaceResolution?> ResolveAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        uint placeNumber,
        ushort level,
        CancellationToken cancellationToken);
}

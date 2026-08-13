using ReferalProgram.Core.PlaceAggregate;

namespace ReferalProgram.Application.Abstractions;

public sealed record SourcePlaceResolution(
    uint Code,
    PlaceResponse SourcePlace);

public interface ISourcePlaceResolver
{
    Task<SourcePlaceResolution?> ResolveAsync(
        Place place,
        byte structureHeight,
        CancellationToken cancellationToken);
}

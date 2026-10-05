using Common.Domain;

namespace ReferalProgram.Core.PlaceAggregate;

public interface IPlaceRepository : IRepository<Place>
{
    Task<IReadOnlyList<Place>> GetStructurePlacesAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    Task<IReadOnlyDictionary<string, string?>> GetInvitersAsync(
        string marketingAddr,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    Task<Place?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Place?> GetAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        uint placeNumber,
        CancellationToken cancellationToken);

    Task<uint> GetNextPlaceNumberAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken);

    Task<long> CountAtDepthAsync(
        string marketingAddr,
        byte structureNumber,
        string mpPrefix,
        uint depth,
        CancellationToken cancellationToken);

    Task<long> CountCloneChildrenAsync(
        int parentId,
        CancellationToken cancellationToken);

    Task IncrementMatrixFillingForAncestorsAsync(
        int parentId,
        CancellationToken cancellationToken);

    void Add(Place place);

    Task RemoveRangeAsync(
        IReadOnlyCollection<Place> places,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

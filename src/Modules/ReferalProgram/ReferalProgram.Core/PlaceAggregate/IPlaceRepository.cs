using Common.Domain;

namespace ReferalProgram.Core.PlaceAggregate;

public interface IPlaceRepository : IRepository<Place>
{
    Task<Place?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Place?> GetAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        uint placeNumber,
        CancellationToken cancellationToken);

    Task<Place?> GetByTaskKeyAsync(
        string marketingAddr,
        int taskKey,
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

    void Add(Place place);
}

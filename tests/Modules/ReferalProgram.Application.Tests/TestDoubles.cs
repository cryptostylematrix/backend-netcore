using Common.Dto;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

internal abstract class PlaceQueriesStub : IPlaceQueries
{
    public virtual Task<PlaceResponse?> GetFirstPlaceAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<PlaceResponse?> GetLastPlaceAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<Paginated<PlaceResponse>> GetPlacesAsync(string marketingAddr, byte structureNumber, string profileAddr, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<long> GetPlacesCountAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<PlaceSubtreeCounts> GetPlaceSubtreeCountsAsync(string marketingAddr, byte structureNumber, string mpPrefix, byte height, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<IReadOnlyDictionary<byte, long>> GetPlaceCountsByPosGroupAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<IReadOnlyList<PlaceResponse>> GetUnfilledPlacesInDepthWindowAsync(string marketingAddr, byte structureNumber, string rootMp, byte width, byte depthSpread, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<PlaceResponse?> GetFirstActiveUnfilledPlaceAsync(string marketingAddr, byte structureNumber, string rootMp, byte width, bool profiledPlacesPrioritized, byte depthSpread, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<IReadOnlyList<PlaceResponse>> GetOpenPlacesByMpPrefixAsync(string marketingAddr, byte structureNumber, string mpPrefix, byte width, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<Paginated<PlaceResponse>> SearchPlacesAsync(string marketingAddr, byte structureNumber, string rootMp, string query, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<PlaceResponse?> GetPlaceByTaskKeyAsync(string marketingAddr, int taskKey, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<PlaceResponse?> GetPlaceAsync(string marketingAddr, byte structureNumber, string? profileAddr, uint placeNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<IReadOnlyList<PlaceResponse>?> GetPathAsync(string marketingAddr, byte structureNumber, string? fromProfileAddr, uint fromPlaceNumber, string? toProfileAddr, uint toPlaceNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<IReadOnlyList<PlaceResponse>> GetPlacesByMpPrefixAsync(string marketingAddr, byte structureNumber, string mpPrefix, byte depthLevels, uint fromPos, uint toPos, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<PlaceResponse?> GetRootPlaceAsync(string marketingAddr, byte structureNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<PlaceResponse?> GetPlaceAsync(int id, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<Paginated<PlaceResponse>> GetChildrenAsync(string marketingAddr, byte structureNumber, string parentProfileAddr, uint parentPlaceNumber, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal abstract class PlaceRepositoryStub : IPlaceRepository
{
    public virtual Task<Place?> GetByIdAsync(int id, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<Place?> GetAsync(string marketingAddr, byte structureNumber, string? profileAddr, uint placeNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<Place?> GetByTaskKeyAsync(string marketingAddr, int taskKey, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<uint> GetNextPlaceNumberAsync(string marketingAddr, byte structureNumber, string? profileAddr, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<Place?> GetAncestorAsync(Place place, byte levels, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual Task<long> CountAtDepthAsync(string marketingAddr, byte structureNumber, string mpPrefix, uint depth, CancellationToken cancellationToken) => throw new NotSupportedException();
    public virtual void Add(Place place) => throw new NotSupportedException();
}

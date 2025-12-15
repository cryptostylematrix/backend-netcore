using Matrix.Domain.Aggregates;

namespace Matrix.Domain.Abstractions;

public record PlacesResult(MultiPlace[] Items,  int Total);

public interface IMultiPlaceRepository: IRepository<MultiPlace>
{
    Task<PlacesResult> GetPlaces(int m, string profileAddr, int page, int pageSize);
    Task<int> GetPlacesCount(int m, string profileAddr);
    Task<MultiPlace?> GetRootPlace(int m, string profileAddr);
    Task<MultiPlace?> GetPlaceByTaskKey(int taskKey);
    Task<int> GetMaxPlaceNumber(int m, string profileAddr);
    Task IncrementFilling(int id);
    Task IncrementFilling2(int id);
    Task<MultiPlace> UpdatePlaceAddressAndConfirm(int id, string addr);
    Task<PlacesResult> SearchPlaces(int m, string profileAddr, string query, int page, int pageSize);
    Task<MultiPlace?> GetPlaceByAddress(string addr);
    Task<int> GetPlacesCountByMpPrefix(int m, string mpPrefix);
    Task<PlacesResult> GetPlacesByMpPrefix(int m, string mpPrefix, int depthLevels, int page, int pageSize);
    Task<MultiPlace?> GetPlaceByMp(int m, string mp);
    Task<PlacesResult> GetOpenPlacesByMpPrefix(int m, string mpPrefix, int page, int pageSize);
    Task<MultiPlace> AddPlace(MultiPlace multiPlace);
}
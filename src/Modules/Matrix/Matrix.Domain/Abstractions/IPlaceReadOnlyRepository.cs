namespace Matrix.Domain.Abstractions;

public record PlacesResult(IEnumerable<MultiPlace> Items,  long Total);

public interface IPlaceReadOnlyRepository
{
    Task<PlacesResult> GetPlaces(short m, string profileAddr, int page, int pageSize);
    Task<long> GetPlacesCount(short m, string profileAddr);
    Task<MultiPlace?> GetRootPlace(short m, string profileAddr);
    Task<MultiPlace?> GetPlaceByTaskKey(int taskKey);
    Task<int> GetMaxPlaceNumber(short m, string profileAddr);
    Task<PlacesResult> SearchPlaces(short m, string profileAddr, string query, int page, int pageSize);
    Task<MultiPlace?> GetPlaceByAddress(string addr);
    Task<long> GetPlacesCountByMpPrefix(short m, string mpPrefix);
    Task<PlacesResult> GetPlacesByMpPrefix(short m, string mpPrefix, int depthLevels, int page, int pageSize);
    Task<MultiPlace?> GetPlaceByMp(short m, string mp);
    Task<PlacesResult> GetOpenPlacesByMpPrefix(short m, string mpPrefix, int page, int pageSize);
}
namespace Matrix.Application.Abstractions;

public interface IPlaceQueries
{
    Task<long> GetPlacesCountAsync(
        short m,
        string profileAddr,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlaceResponse>> GetOpenPlacesByMpPrefixAsync(
        short m,
        string mpPrefix,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PlaceResponse?> GetPlaceByAddressAsync(
        string addr,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlaceResponse>?> GetPathAsync(
        string rootAddr,
        string placeAddr,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlaceResponse>> GetPlacesByMpPrefixAsync(
        short m,
        string mpPrefix,
        int depthLevels,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<long> GetPlacesCountByMpPrefixAsync(
        short m,
        string mpPrefix,
        CancellationToken cancellationToken);

    Task<Paginated<PlaceResponse>> GetPlacesAsync(
        short m,
        string profileAddr,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PlaceResponse?> GetRootPlaceAsync(
        short m,
        string profileAddr,
        CancellationToken cancellationToken);

    Task<Paginated<PlaceResponse>> SearchPlacesAsync(
        short m,
        string profileAddr,
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
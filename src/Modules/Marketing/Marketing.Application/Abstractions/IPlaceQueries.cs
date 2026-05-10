namespace Marketing.Application.Abstractions;

public interface IPlaceQueries
{
    Task<long> GetPlacesCountAsync(
        string marketingAddr,
        byte m,
        string profileAddr,
        CancellationToken cancellationToken);
    
    Task<long> GetPlacesTotalCountAsync(
        string marketingAddr,
        string profileAddr,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlaceResponse>> GetOpenPlacesByMpPrefixAsync(
        string marketingAddr,
        byte m,
        string mpPrefix,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PlaceResponse?> GetPlaceByAddressAsync(
        string marketingAddr,
        string placeAddr,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlaceResponse>?> GetPathAsync(
        string marketingAddr,
        string rootAddr,
        string placeAddr,
        CancellationToken cancellationToken);

    // Task<IReadOnlyList<PlaceResponse>> GetPlacesByMpPrefixAsync(
    //     string marketingAddr,
    //     byte m,
    //     string mpPrefix,
    //     int depthLevels,
    //     int page,
    //     int pageSize,
    //     CancellationToken cancellationToken);

    Task<IReadOnlyList<PlaceResponse>> GetPlacesByMpPrefixAsync(
        string marketingAddr,
        byte m,
        string mpPrefix,
        int depthLevels,
        uint fromPos,
        uint toPos,
        CancellationToken ct);

    Task<long> GetPlacesCountByMpPrefixAsync(
        string marketingAddr,
        byte m,
        string mpPrefix,
        CancellationToken cancellationToken);

    Task<Paginated<PlaceResponse>> GetPlacesAsync(
        string marketingAddr,
        byte m,
        string profileAddr,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PlaceResponse?> GetRootPlaceAsync(
        string marketingAddr,
        byte m,
        string profileAddr,
        CancellationToken cancellationToken);

    Task<Paginated<PlaceResponse>> SearchPlacesAsync(
        string marketingAddr,
        byte m,
        string profileAddr,
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PlaceResponse?> GetPlaceByTaskKeyAsync(
        string marketingAddr,
        uint taskKey,
        CancellationToken ct);

    Task<uint> GetMaxPlaceNumberAsync(
        string marketingAddr,
        byte m,
        string profileAddr,
        CancellationToken ct);
}
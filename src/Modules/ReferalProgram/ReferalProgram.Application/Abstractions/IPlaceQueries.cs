namespace ReferalProgram.Application.Abstractions;

public interface IPlaceQueries
{
    Task<PlaceResponse?> GetFirstPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken);

    Task<PlaceResponse?> GetLastPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken);

    Task<Paginated<PlaceResponse>> GetPlacesAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<long> GetPlacesCountAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<byte, long>> GetPlaceCountsByPosGroupAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlaceResponse>> GetUnfilledPlacesAtMinDepthAsync(
        string marketingAddr,
        byte structureNumber,
        string rootMp,
        byte width,
        CancellationToken cancellationToken);

    Task<PlaceResponse?> GetFirstActiveUnfilledPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        string rootMp,
        byte width,
        CancellationToken cancellationToken);

    Task<Paginated<PlaceResponse>> SearchPlacesAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PlaceResponse?> GetPlaceByTaskKeyAsync(
        string marketingAddr,
        int taskKey,
        CancellationToken cancellationToken);

    Task<PlaceResponse?> GetPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        uint placeNumber,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlaceResponse>?> GetPathAsync(
        string marketingAddr,
        byte structureNumber,
        string? fromProfileAddr,
        uint fromPlaceNumber,
        string? toProfileAddr,
        uint toPlaceNumber,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlaceResponse>> GetPlacesByMpPrefixAsync(
        string marketingAddr,
        byte structureNumber,
        string mpPrefix,
        byte depthLevels,
        uint fromPos,
        uint toPos,
        CancellationToken cancellationToken);

    Task<PlaceResponse?> GetRootPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken);

    Task<PlaceResponse?> GetPlaceAsync(
        int id,
        CancellationToken cancellationToken);

    Task<Paginated<PlaceResponse>> GetChildrenAsync(
        string marketingAddr,
        byte structureNumber,
        string parentProfileAddr,
        uint parentPlaceNumber,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

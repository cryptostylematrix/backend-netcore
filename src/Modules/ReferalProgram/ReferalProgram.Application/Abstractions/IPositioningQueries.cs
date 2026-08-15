namespace ReferalProgram.Application.Abstractions;

public interface INextPositionQueries
{
    Task<StructureResponse?> GetStructureAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<byte, long>> GetPlaceCountsByPosGroupAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken);
}

public interface IPositionCandidateQueries
{
    Task<IReadOnlyList<PlaceResponse>> GetUnfilledPlacesInDepthWindowAsync(
        string marketingAddr,
        byte structureNumber,
        string rootMp,
        byte width,
        byte depthSpread,
        IReadOnlyCollection<string> lockMps,
        CancellationToken cancellationToken);

    Task<PlaceResponse?> GetFirstActiveUnfilledPlaceAsync(
        string marketingAddr,
        byte structureNumber,
        string rootMp,
        byte width,
        bool profiledPlacesPrioritized,
        byte depthSpread,
        IReadOnlyCollection<string> lockMps,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlaceResponse>> GetOpenPlacesByMpPrefixAsync(
        string marketingAddr,
        byte structureNumber,
        string mpPrefix,
        byte width,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public interface IPositionLockQueries
{
    Task<string[]> GetAllLockMpsAsync(
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        CancellationToken cancellationToken);
}

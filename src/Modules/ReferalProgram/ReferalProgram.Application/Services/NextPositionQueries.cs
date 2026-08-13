namespace ReferalProgram.Application.Services;

public sealed class NextPositionQueries(
    IStructureQueries structureQueries,
    IPlaceQueries placeQueries) : INextPositionQueries
{
    public Task<StructureResponse?> GetStructureAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken) =>
        structureQueries.GetStructureAsync(marketingAddr, structureNumber, cancellationToken);

    public Task<IReadOnlyDictionary<byte, long>> GetPlaceCountsByPosGroupAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken) =>
        placeQueries.GetPlaceCountsByPosGroupAsync(
            marketingAddr,
            structureNumber,
            cancellationToken);
}

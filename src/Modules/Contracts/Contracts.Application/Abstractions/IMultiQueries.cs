namespace Contracts.Application.Abstractions;

public interface IMultiQueries
{
    Task<Result<MinQueueTaskResponse>> GetMinQueueTaskAsync(CancellationToken ct = default);
    Task<Result<MultiDataResponse>> GetMultiDataAsync(CancellationToken ct = default);

    Result<BuyPlaceBodyResponse> BuildBuyPlaceBody(
        long queryId,
        int m,
        string profileAddr,
        string? parentAddr,
        int? pos);
    
    Result<LockPosBodyResponse> BuildLockPosBody(
        long queryId,
        int m,
        string profileAddr,
        string parentAddr,
        int pos);
    
    Result<UnlockPosBodyResponse> BuildUnlockPosBody(
        long queryId,
        int m,
        string profileAddr,
        string parentAddr,
        int pos);
}
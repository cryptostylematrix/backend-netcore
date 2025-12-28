namespace Contracts.Application.Abstractions;

public interface IMultiQueries
{
    Task<Result<MinQueueTaskResponse>> GetMinQueueTaskAsync(CancellationToken ct = default);
    Task<Result<MultiDataResponse>> GetMultiDataAsync(CancellationToken ct = default);
}
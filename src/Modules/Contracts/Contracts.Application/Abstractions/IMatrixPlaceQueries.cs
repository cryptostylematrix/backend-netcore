namespace Contracts.Application.Abstractions;

public interface IMatrixPlaceQueries
{
    Task<Result<MatrixPlaceDataResponse>> GetPlaceDataAsync(string addr, CancellationToken ct = default);
}
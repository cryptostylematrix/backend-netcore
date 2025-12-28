namespace Contracts.Application.Abstractions;

public interface IPlaceQueries
{
    Task<Result<PlaceDataResponse>> GetPlaceDataAsync(string addr, CancellationToken ct = default);
}
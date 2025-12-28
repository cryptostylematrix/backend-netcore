namespace Contracts.Application.Abstractions;

public interface IProfileItemQueries
{
    Task<Result<ProfileDataResponse>> GetNftDataAsync(string addr, CancellationToken ct = default);
    Task<Result<ProfileProgramsResponse>> GetProgramsAsync(string addr, CancellationToken ct = default);
}
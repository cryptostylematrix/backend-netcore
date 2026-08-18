namespace Contracts.Application.Abstractions;

public interface IProfileItemQueries
{
    Task<Result<ProfileDataResponse>> GetNftDataAsync(string addr, CancellationToken ct = default);
    Task<Result<ProfileDataResponse>> GetFreshNftDataAsync(string addr, CancellationToken ct = default);
    Task<Result<ProfileProgramsResponse>> GetProgramsAsync(string addr, CancellationToken ct = default);
    Result<ChooseInviterBodyResponse> BuildChooseInviterBody(
        long queryId,
        uint program, 
        string inviterAddr, 
        int seqNo, 
        string inviteAddr);
    
    Result<EditContentBodyResponse> BuildEditContentBody(
        long queryId,
        string login, 
        string? imageUrl, 
        string? firstName, 
        string? lastName, 
        string? tgUsername);
    
    
    Task<Result> InvalidateNftDataCacheAsync(string addr, CancellationToken ct = default);
    Task<Result> InvalidateProgramsCacheAsync(string addr, CancellationToken ct = default);
}

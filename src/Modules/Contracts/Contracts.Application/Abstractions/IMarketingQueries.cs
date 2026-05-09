namespace Contracts.Application.Abstractions;

public interface IMarketingQueries
{
    Task<Result<MarketingTransactionHistoryResponse>> GetMarketingHistoryAsync(
        string addr, 
        uint limit,
        ulong? lt,
        string? hash,
        CancellationToken ct = default);
    
    Result<BuyPlaceByTonBodyResponse> BuildBuyPlaceByTonBody(
        long queryId,
        int m,
        string profileAddr,
        bool first,
        string? parentAddr,
        int? pos);
    
    Result<BuyPlaceByJettonBodyResponse> BuildBuyPlaceByJettonBody(
        long queryId,
        string marketingAddr,
        int m,
        string profileAddr,
        bool first,
        string? parentAddr,
        int? pos,
        ulong amount,
        string senderAddr,
        ulong fee);
    
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
    
    Task<Result<FirstTaskResponse>> GetFirstTaskAsync(string marketingAddr, CancellationToken ct = default);
    Task<Result<MarketingDataResponse>> GetMarketingDataAsync(string marketingAddr, CancellationToken ct = default);
}
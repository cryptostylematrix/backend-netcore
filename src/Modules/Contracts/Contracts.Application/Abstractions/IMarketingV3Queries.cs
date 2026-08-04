namespace Contracts.Application.Abstractions;

public interface IMarketingV3Queries
{
    Result<MarketingV3MessageBodyResponse> BuildExecMessageBody(
        ulong queryId,
        byte structure,
        string profileAddr,
        uint commandTag,
        string? payloadBocHex);

    Result<MarketingV3MessageBodyResponse> SendCommandResponse(
        ulong queryId,
        uint taskKey,
        uint code,
        MarketingV3SourcePlace source);

    Result<MarketingV3MessageBodyResponse> SendBonusQueryResponse(
        ulong queryId,
        uint taskKey,
        MarketingV3PlaceInfo reason,
        MarketingV3ProfileData recipient);

    Result<MarketingV3MessageBodyResponse> SendProfileInfoQueryResponse(
        ulong queryId,
        uint taskKey,
        MarketingV3ProfileInfo profile);

    Result<MarketingV3MessageBodyResponse> SendCancelTask(
        ulong queryId,
        uint taskKey,
        string comment);

    Task<Result<MarketingV3DataResponse>> GetMarketingDataAsync(
        string marketingAddr,
        CancellationToken ct = default);

    Task<Result<MarketingV3BasicDataResponse>> GetBasicDataAsync(
        string marketingAddr,
        CancellationToken ct = default);

    Task<Result<MarketingV3FirstTaskResponse>> GetFirstTaskAsync(
        string marketingAddr,
        CancellationToken ct = default);
}
